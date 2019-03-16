namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public static partial class Comper
    {
        private struct LZSSGraphEdge
        {
            public int cost;
            public int next_node_index;
            public int previous_node_index;
            public int match_length;
            public int match_offset;
        };

        internal static void Encode(Stream source, Stream destination)
        {
            int size_bytes = (int)(source.Length - source.Position);
            byte[] buffer_bytes = new byte[size_bytes + (size_bytes & 1)];
            source.Read(buffer_bytes, 0, size_bytes);

            int size = (size_bytes + 1) / 2;
            ushort[] buffer = new ushort[size];
            for (int i = 0; i < size; ++i)
            {
                buffer[i] = (ushort)((buffer_bytes[i * 2] << 8) | buffer_bytes[(i * 2) + 1]);
            }

            /*
             * Here we create and populate the "LZSS graph":
             * 
             * Each value in the uncompressed file forms a node in this graph.
             * The various edges between these nodes represent LZSS matches.
             * 
             * Using a shortest-path algorithm, these edges can be used to
             * find the optimal combination of matches needed to produce the
             * smallest possible file.
             * 
             * The outputted array only contains one edge per node: the optimal
             * one. This means, in order to produce the smallest file, you just
             * have to traverse the graph from one edge to the next, encoding
             * each match as you go along.
            */

            LZSSGraphEdge[] node_meta_array = new LZSSGraphEdge[size + 1];

            // Initialise the array
            node_meta_array[0].cost = 0;
            for (int i = 1; i < size + 1; ++i)
                node_meta_array[i].cost = int.MaxValue;

            // Find matches
            for (int i = 0; i < size; ++i)
            {
                int max_read_ahead = Math.Min(0x100, size - i);
                int max_read_behind = Math.Max(0, i - 0x100);

                // Search for dictionary matches
                for (int j = i; j-- > max_read_behind;)
                {
                    for (int k = 0; k < max_read_ahead; ++k)
                    {
                        if (buffer[i + k] == buffer[j + k])
                        {
                            int distance = i - j;
                            int length = k + 1;

                            // Update this node's optimal edge if this one is better
                            if (node_meta_array[i + k + 1].cost > node_meta_array[i].cost + 1 + 16)
                            {
                                node_meta_array[i + k + 1].cost = node_meta_array[i].cost + 1 + 16;
                                node_meta_array[i + k + 1].previous_node_index = i;
                                node_meta_array[i + k + 1].match_length = k + 1;
                                node_meta_array[i + k + 1].match_offset = j;
                            }
                        }
                        else
                            break;
                    }
                }

                // Do literal match
                // Update this node's optimal edge if this one is better (or the same, since literal matches usually decode faster)
                if (node_meta_array[i + 1].cost >= node_meta_array[i].cost + 1 + 16)
                {
                    node_meta_array[i + 1].cost = node_meta_array[i].cost + 1 + 16;
                    node_meta_array[i + 1].previous_node_index = i;
                    node_meta_array[i + 1].match_length = 0;
                }
            }

            // Reverse the edge link order, so the array can be traversed from start to end, rather than vice versa
            node_meta_array[0].previous_node_index = int.MaxValue;
            node_meta_array[size].next_node_index = int.MaxValue;
            for (int node_index = size; node_meta_array[node_index].previous_node_index != int.MaxValue; node_index = node_meta_array[node_index].previous_node_index)
                node_meta_array[node_meta_array[node_index].previous_node_index].next_node_index = node_index;

            /*
             * LZSS graph complete
             */

            UInt16BE_NE_H_OutputBitStream bitStream = new UInt16BE_NE_H_OutputBitStream(destination);
            MemoryStream data = new MemoryStream();

            for (int node_index = 0; node_meta_array[node_index].next_node_index != int.MaxValue; node_index = node_meta_array[node_index].next_node_index)
            {
                int next_index = node_meta_array[node_index].next_node_index;

                int length = node_meta_array[next_index].match_length;
                int distance = next_index - node_meta_array[next_index].match_length - node_meta_array[next_index].match_offset;

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
                    BigEndian.Write2(data, buffer[node_index]);
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

            for (; ; )
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

                    for (int i = 0; i <= length; i++)
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
