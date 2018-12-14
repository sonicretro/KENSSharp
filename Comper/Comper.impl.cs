namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;
    using System.Security;

    public static partial class Comper
    {
        private static void FindExtraMatches(ushort[] data, long pos, long data_size, long offset, LZSS.NodeMeta[] node_meta_array)
        {
            // Comper has no special matches
        }

        private static long GetMatchCost(long distance, long length)
        {
            return 1 + 16;
        }

        internal static void Encode(Stream source, Stream destination)
        {
            long size_bytes = source.Length - source.Position;
            byte[] buffer_bytes = new byte[size_bytes + (size_bytes & 1)];
            source.Read(buffer_bytes, 0, (int)size_bytes);

            long size = (size_bytes + 1) / 2;
            ushort[] buffer = new ushort[size];
            for(long i = 0; i < size; ++i)
            {
                buffer[i] = (ushort)((buffer_bytes[i * 2] << 8) | buffer_bytes[(i * 2) + 1]);
            }

            LZSS.NodeMeta[] node_meta_array = LZSS.FindMatches(buffer, 0, size, 0x100, 0x100, FindExtraMatches, 1 + 16, GetMatchCost);

            UInt16BE_NE_H_OutputBitStream bitStream = new UInt16BE_NE_H_OutputBitStream(destination);
            MemoryStream data = new MemoryStream();

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
