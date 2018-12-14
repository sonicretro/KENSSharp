namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;
    using System.Security;

    public static partial class Comper
    {
        private struct LZSSNodeMeta
        {
            public long cost;
            public long next_node_index;
            public long previous_node_index;
            public long match_length;
            public long match_offset;
        };

        private const long max_match_length = 0x100;
        private const long max_match_distance = 0x100;
        private const long literal_cost = 1 + 16;

        private static long GetMatchCost(long distance, long length)
        {
            return 1 + 16;
        }

        internal static void Encode(Stream source, Stream destination)
        {
            long size = source.Length - source.Position;
            byte[] buffer = new byte[size + (size & 1)];
            source.Read(buffer, 0, (int)size);

            EncodeInternal(destination, buffer, size / 2);
        }

        private static void EncodeInternal(Stream destination, byte[] buffer, long size)
        {
            UInt16BE_NE_H_OutputBitStream bitStream = new UInt16BE_NE_H_OutputBitStream(destination);
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
                        if (buffer[(i + k) * 2] == buffer[(j + k) * 2] && buffer[((i + k) * 2) + 1] == buffer[((j + k) * 2) + 1])
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
                    // Compressed
                    Push(bitStream, true, destination, data);
                    NeutralEndian.Write1(data, (byte)-distance);
                    NeutralEndian.Write1(data, (byte)(length - 1));
                }
                else
                {
                    // Uncompressed
                    Push(bitStream, false, destination, data);
                    NeutralEndian.Write1(data, buffer[node_index * 2]);
                    NeutralEndian.Write1(data, buffer[(node_index * 2) + 1]);
                }
            }

            Push(bitStream, true, destination, data);

            NeutralEndian.Write1(data, 0);
            NeutralEndian.Write1(data, 0);
            bitStream.Flush(true);

            byte[] bytes = data.ToArray();
            destination.Write(bytes, 0, bytes.Length);
        }

        private static void Push(UInt16BE_NE_H_OutputBitStream bitStream, bool bit, Stream destination, MemoryStream data)
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
            DecodeInternal(source, destination);
        }

        private static void DecodeInternal(Stream source, Stream destination)
        {
            UInt16BE_NE_H_InputBitStream bitStream = new UInt16BE_NE_H_InputBitStream(source);

            for (;;)
            {
                if (!bitStream.Pop())
                {
                    // Symbolwise match
                    ushort word = BigEndian.Read2(source);
                    BigEndian.Write2(destination, word);
                }
                else
                {
                    // Dictionary match
                    int distance = (0x100 - NeutralEndian.Read1(source)) * 2;
                    int length = NeutralEndian.Read1(source);
                    if (length == 0)
                    {
                        // End-of-stream marker
                        break;
                    }

                    for (long i = 0; i <= length; i++)
                    {
                        long writePosition = destination.Position;
                        destination.Seek(writePosition - distance, SeekOrigin.Begin);
                        ushort s = BigEndian.Read2(destination);
                        destination.Seek(writePosition, SeekOrigin.Begin);
                        BigEndian.Write2(destination, s);
                    }
                }
            }
        }
    }
}
