namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public static partial class Kosinski
    {
        private static void FindExtraMatches(byte[] data, long data_size, long offset, LZSS.NodeMeta[] node_meta_array)
        {
            // Kosinski has no special matches
        }

        private static long GetMatchCost(long distance, long length)
        {
            if (length >= 2 && length <= 5 && distance <= 256)
                return 2 + 2 + 8;   // Descriptor bits, length bits, offset byte
            else if (length >= 3 && length <= 9)
                return 2 + 16;      // Descriptor bits, offset/length bytes
            else if (length >= 3)
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

        internal static void Encode(Stream source, Stream destination, Endianness headerEndianness)
        {
            long size = source.Length - source.Position;
            byte[] buffer = new byte[size];
            source.Read(buffer, 0, (int)size);

            long pos = 0;
            if (size > 0xffff)
            {
                throw new CompressionException(Properties.Resources.KosinskiTotalSizeTooLarge);
            }

            long remainingSize = size;
            long compBytes = 0;

            if (remainingSize > 0x1000)
            {
                remainingSize = 0x1000;
            }

            if (headerEndianness == Endianness.BigEndian)
            {
                BigEndian.Write2(destination, (ushort)size);
            }
            else
            {
                LittleEndian.Write2(destination, (ushort)size);
            }

            for (;;)
            {
                EncodeInternal(destination, buffer, pos, remainingSize);

                compBytes += remainingSize;
                pos += remainingSize;

                if (compBytes >= size)
                {
                    break;
                }

                // Padding between modules
                long paddingEnd = (((destination.Position - 2) + 0xF) & ~0xF) + 2;
                while (destination.Position < paddingEnd)
                {
                    destination.WriteByte(0);
                }

                remainingSize = Math.Min(0x1000L, size - compBytes);
            }
        }

        private static void EncodeInternal(Stream destination, byte[] buffer, long pos, long size)
        {
            LZSS.NodeMeta[] node_meta_array = LZSS.FindMatches(buffer, size, 0x100, 0x2000, FindExtraMatches, 1 + 8, GetMatchCost);

            UInt16LE_E_L_OutputBitStream bitStream = new UInt16LE_E_L_OutputBitStream(destination);
            MemoryStream data = new MemoryStream();

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
                        Push(bitStream, ((length - 2) & 2) != 0, destination, data);
                        Push(bitStream, ((length - 2) & 1) != 0, destination, data);
                        NeutralEndian.Write1(data, (byte)-distance);
                    }
                    else if (length >= 3 && length <= 9)
                    {
                        Push(bitStream, false, destination, data);
                        Push(bitStream, true, destination, data);
                        NeutralEndian.Write1(data, (byte)(-distance & 0xFF));
                        NeutralEndian.Write1(data, (byte)(((-distance >> (8 - 3)) & 0xF8) | ((length - 2) & 7)));
                    }
                    else //if (length >= 3)
                    {
                        Push(bitStream, false, destination, data);
                        Push(bitStream, true, destination, data);
                        NeutralEndian.Write1(data, (byte)(-distance & 0xFF));
                        NeutralEndian.Write1(data, (byte)((-distance >> (8 - 3)) & 0xF8));
                        NeutralEndian.Write1(data, (byte)(length - 1));
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

            // If the bit stream was just flushed, write an empty bit stream that will be read just before the end-of-data
            // sequence below.
            if (!bitStream.HasWaitingBits)
            {
                NeutralEndian.Write1(data, 0);
                NeutralEndian.Write1(data, 0);
            }

            NeutralEndian.Write1(data, 0);
            NeutralEndian.Write1(data, 0xF0);
            NeutralEndian.Write1(data, 0);
            bitStream.Flush(true);

            byte[] bytes = data.ToArray();
            destination.Write(bytes, 0, bytes.Length);
        }

        private static void Push(UInt16LE_E_L_OutputBitStream bitStream, bool bit, Stream destination, MemoryStream data)
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

        internal static void Decode(Stream source, Stream destination, Endianness headerEndianness)
        {
            long decompressedBytes = 0;
            long fullSize;
            if (headerEndianness == Endianness.BigEndian)
            {
                fullSize = BigEndian.Read2(source);
            }
            else
            {
                fullSize = LittleEndian.Read2(source);
            }

            for (;;)
            {
                DecodeInternal(source, destination, ref decompressedBytes);
                if (decompressedBytes >= fullSize)
                {
                    break;
                }

                // Skip the padding between modules
                int b;
                long paddingEnd = (((source.Position - 2) + 0xF) & ~0xF) + 2;
                while (source.Position < paddingEnd)
                {
                    b = source.ReadByte();

                    if (b == -1)
                    {
                        throw new EndOfStreamException();
                    }
                }
            }
        }

        private static void DecodeInternal(Stream source, Stream destination, ref long decompressedBytes)
        {
            UInt16LE_E_L_InputBitStream bitStream = new UInt16LE_E_L_InputBitStream(source);

            for (;;)
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
                        byte low = NeutralEndian.Read1(source);
                        byte high = NeutralEndian.Read1(source);
                        count = high & 0x07;
                        if (count == 0)
                        {
                            count = NeutralEndian.Read1(source);
                            if (count == 0)
                            {
                                break;
                            }

                            if (count == 1)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            ++count;
                        }

                        offset = ~0x1FFFL | ((0xF8 & high) << 5) | low;
                    }
                    else
                    {
                        byte low = Convert.ToByte(bitStream.Pop());
                        byte high = Convert.ToByte(bitStream.Pop());
                        count = (low << 1 | high) + 1;
                        offset = NeutralEndian.Read1(source);
                        offset |= ~0xFFL;
                    }

                    for (long i = 0; i <= count; i++)
                    {
                        long writePosition = destination.Position;
                        destination.Seek(writePosition + offset, SeekOrigin.Begin);
                        byte b = NeutralEndian.Read1(destination);
                        destination.Seek(writePosition, SeekOrigin.Begin);
                        NeutralEndian.Write1(destination, b);
                    }

                    decompressedBytes += count + 1;
                }
            }
        }
    }
}
