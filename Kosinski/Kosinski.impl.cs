namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public static partial class Kosinski
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
            int size = (int)(source.Length - source.Position);
            byte[] buffer = new byte[size];
            source.Read(buffer, 0, size);

            EncodeInternal(destination, buffer, 0, size);
        }

        internal static void Encode(Stream source, Stream destination, Endianness headerEndianness)
        {
            int size = (int)(source.Length - source.Position);
            byte[] buffer = new byte[size];
            source.Read(buffer, 0, size);

            int pos = 0;
            if (size > 0xffff)
            {
                throw new CompressionException(Properties.Resources.KosinskiTotalSizeTooLarge);
            }

            int remainingSize = size;
            int compBytes = 0;

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

            for (; ; )
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

                remainingSize = Math.Min(0x1000, size - compBytes);
            }
        }

        private static void EncodeInternal(Stream destination, byte[] buffer, int pos, int size)
        {
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
                int max_read_behind = Math.Max(0, i - 0x2000);

                // Search for dictionary matches
                for (int j = i; j-- > max_read_behind;)
                {
                    for (int k = 0; k < max_read_ahead; ++k)
                    {
                        if (buffer[pos + i + k] == buffer[pos + j + k])
                        {
                            int distance = i - j;
                            int length = k + 1;

                            // Get the cost of the match (or bail if it can't be compressed)
                            int cost;
                            if (length >= 2 && length <= 5 && distance <= 256)
                                cost = 2 + 2 + 8;   // Descriptor bits, length bits, offset byte
                            else if (length >= 3 && length <= 9)
                                cost = 2 + 16;      // Descriptor bits, offset/length bytes
                            else if (length >= 3)
                                cost = 2 + 16 + 8;  // Descriptor bits, offset bytes, length byte
                            else
                                continue;           // In the event a match cannot be compressed

                            // Update this node's optimal edge if this one is better
                            if (node_meta_array[i + k + 1].cost > node_meta_array[i].cost + cost)
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

                // Do literal match
                // Update this node's optimal edge if this one is better (or the same, since literal matches usually decode faster)
                if (node_meta_array[i + 1].cost >= node_meta_array[i].cost + 1 + 8)
                {
                    node_meta_array[i + 1].cost = node_meta_array[i].cost + 1 + 8;
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

            UInt16LE_E_L_OutputBitStream bitStream = new UInt16LE_E_L_OutputBitStream(destination);
            MemoryStream data = new MemoryStream();

            for (int node_index = 0; node_meta_array[node_index].next_node_index != int.MaxValue; node_index = node_meta_array[node_index].next_node_index)
            {
                int next_index = node_meta_array[node_index].next_node_index;

                int length = node_meta_array[next_index].match_length;
                int distance = next_index - node_meta_array[next_index].match_length - node_meta_array[next_index].match_offset;

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

            for (; ; )
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
