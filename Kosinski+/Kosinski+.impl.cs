namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public static partial class KosinskiPlus
    {
        private struct LZSSNodeMeta
        {
            public long cost;
            public long next_node_index;
            public long previous_node_index;
            public long match_length;
            public long match_offset;
        };

        private const long max_match_length = 0x100 + 8;
        private const long max_match_distance = 0x2000;
        private const long literal_cost = 1 + 8;

        private static long GetMatchCost(long distance, long length)
        {
            if (length >= 2 && length <= 5 && distance <= 256)
                return 2 + 2 + 8;   // Descriptor bits, length bits, offset byte
            else if (length >= 3 && length <= 9)
                return 2 + 16;      // Descriptor bits, offset/length bytes
            else if (length >= 10)
                return 2 + 16 + 8;  // Descriptor bits, offset bytes, length byte
            else
                return 0; 		// In the event a match cannot be compressed
        }

        internal static void Encode(Stream source, Stream destination)
        {
            long size = source.Length - source.Position;
            byte[] buffer = new byte[size];
            source.Read(buffer, 0, (int)size);

            EncodeInternal(destination, buffer, 0, size);
        }

        internal static void EncodeModuled(Stream source, Stream destination)
        {
            long size = source.Length - source.Position;
            byte[] buffer = new byte[size];
            source.Read(buffer, 0, (int)size);

            long pos = 0;
            if (size > 0xffff)
            {
                throw new CompressionException(Properties.Resources.KosinskiPlusTotalSizeTooLarge);
            }

            long remainingSize = size;
            long compBytes = 0;

            if (remainingSize > 0x1000)
            {
                remainingSize = 0x1000;
            }

            BigEndian.Write2(destination, (ushort)size);

            for (; ; )
            {
                EncodeInternal(destination, buffer, pos, remainingSize);

                compBytes += remainingSize;
                pos += remainingSize;

                if (compBytes >= size)
                {
                    break;
                }

                remainingSize = Math.Min(0x1000L, size - compBytes);
            }
        }

        private static void EncodeInternal(Stream destination, byte[] buffer, long pos, long size)
        {
            UInt8_NE_H_OutputBitStream bitStream = new UInt8_NE_H_OutputBitStream(destination);
            MemoryStream data = new MemoryStream();

            LZSSNodeMeta[] node_meta_array = new LZSSNodeMeta[size + 1];

            node_meta_array[0].cost = 0;
            for (long i = 1; i < size + 1; ++i)
                node_meta_array[i].cost = long.MaxValue;

            for (long i = 0; i < size; ++i)
            {
                long max_read_ahead = Math.Min(max_match_length, size - i);
                long max_read_behind = max_match_distance > i ? 0 : i - max_match_distance;

                for (long j = i; j-- > max_read_behind;)
                {
                    for (long k = 0; k < max_read_ahead; ++k)
                    {
                        if (buffer[pos + i + k] == buffer[pos + j + k])
                        {
                            long cost = GetMatchCost(i - j, k + 1);

                            if (cost != 0 && node_meta_array[i + k + 1].cost > node_meta_array[i].cost + cost)
                            {
                                node_meta_array[i + k + 1].cost = node_meta_array[i].cost + cost;
                                node_meta_array[i + k + 1].previous_node_index = i;
                                node_meta_array[i + k + 1].match_length = k + 1;
                                node_meta_array[i + k + 1].match_offset = j;
                            }
                        }
                        else
                            break;
                    }
                }

                if (node_meta_array[i + 1].cost >= node_meta_array[i].cost + literal_cost)
                {
                    node_meta_array[i + 1].cost = node_meta_array[i].cost + literal_cost;
                    node_meta_array[i + 1].previous_node_index = i;
                    node_meta_array[i + 1].match_length = 0;
                }
            }

            node_meta_array[0].previous_node_index = long.MaxValue;
            node_meta_array[size].next_node_index = long.MaxValue;
            for (long node_index = size; node_meta_array[node_index].previous_node_index != long.MaxValue; node_index = node_meta_array[node_index].previous_node_index)
                node_meta_array[node_meta_array[node_index].previous_node_index].next_node_index = node_index;

            for (long node_index = 0; node_meta_array[node_index].next_node_index != long.MaxValue; node_index = node_meta_array[node_index].next_node_index)
            {
                long next_index = node_meta_array[node_index].next_node_index;

                long length = node_meta_array[next_index].match_length;
                long distance = next_index - node_meta_array[next_index].match_length - node_meta_array[next_index].match_offset;

                if (length != 0)
                {
                    if (length >= 2 && length <= 5 && distance <= 256)
                    {
                        Push(bitStream, false, destination, data);
                        Push(bitStream, false, destination, data);
                        NeutralEndian.Write1(data, (byte)-distance);
                        Push(bitStream, ((length - 2) & 2) != 0, destination, data);
                        Push(bitStream, ((length - 2) & 1) != 0, destination, data);
                    }
                    else if (length >= 3 && length <= 9)
                    {
                        Push(bitStream, false, destination, data);
                        Push(bitStream, true, destination, data);
                        NeutralEndian.Write1(data, (byte)(((-distance >> (8 - 3)) & 0xF8) | ((10 - length) & 7)));
                        NeutralEndian.Write1(data, (byte)(-distance & 0xFF));
                    }
                    else //if (length >= 3)
                    {
                        Push(bitStream, false, destination, data);
                        Push(bitStream, true, destination, data);
                        NeutralEndian.Write1(data, (byte)((-distance >> (8 - 3)) & 0xF8));
                        NeutralEndian.Write1(data, (byte)(-distance & 0xFF));
                        NeutralEndian.Write1(data, (byte)(length - 9));
                    }
                }
                else
                {
                    Push(bitStream, true, destination, data);
                    NeutralEndian.Write1(data, buffer[pos + node_index]);
                }
            }

            Push(bitStream, false, destination, data);
            Push(bitStream, true, destination, data);

            NeutralEndian.Write1(data, 0xF0);
            NeutralEndian.Write1(data, 0);
            NeutralEndian.Write1(data, 0);
            bitStream.Flush(true);

            byte[] bytes = data.ToArray();
            destination.Write(bytes, 0, bytes.Length);
        }

        private static void Push(UInt8_NE_H_OutputBitStream bitStream, bool bit, Stream destination, MemoryStream data)
        {
            if (bitStream.Push(bit))
            {
                byte[] bytes = data.ToArray();
                destination.Write(bytes, 0, bytes.Length);
                data.SetLength(0);
            }
        }

        internal static void Decode(Stream source, Stream destination)
        {
            long decompressedBytes = 0;
            DecodeInternal(source, destination, ref decompressedBytes);
        }

        internal static void DecodeModuled(Stream source, Stream destination)
        {
            long decompressedBytes = 0;
            long fullSize = BigEndian.Read2(source);

            for (; ; )
            {
                DecodeInternal(source, destination, ref decompressedBytes);
                if (decompressedBytes >= fullSize)
                {
                    break;
                }
            }
        }

        private static void DecodeInternal(Stream source, Stream destination, ref long decompressedBytes)
        {
            UInt8_NE_H_InputBitStream bitStream = new UInt8_NE_H_InputBitStream(source);

            for (; ; )
            {
                if (bitStream.Pop())
                {
                    NeutralEndian.Write1(destination, NeutralEndian.Read1(source));
                    ++decompressedBytes;
                }
                else
                {
                    long count = 0;
                    long offset = 0;

                    if (bitStream.Pop())
                    {
                        byte high = NeutralEndian.Read1(source);
                        byte low = NeutralEndian.Read1(source);
                        count = high & 0x07;
                        if (count == 0)
                        {
                            count = NeutralEndian.Read1(source);
                            if (count == 0)
                            {
                                break;
                            }

                            count += 9;
                        }
                        else
                        {
                            count = 10 - count;
                        }

                        offset = ~0x1FFFL | ((0xF8 & high) << 5) | low;
                    }
                    else
                    {
                        offset = NeutralEndian.Read1(source);
                        offset |= ~0xFFL;
                        byte low = Convert.ToByte(bitStream.Pop());
                        byte high = Convert.ToByte(bitStream.Pop());
                        count = (low << 1 | high) + 2;
                    }

                    for (long i = 0; i < count; i++)
                    {
                        long writePosition = destination.Position;
                        destination.Seek(writePosition + offset, SeekOrigin.Begin);
                        byte b = NeutralEndian.Read1(destination);
                        destination.Seek(writePosition, SeekOrigin.Begin);
                        NeutralEndian.Write1(destination, b);
                    }

                    decompressedBytes += count;
                }
            }
        }
    }
}
