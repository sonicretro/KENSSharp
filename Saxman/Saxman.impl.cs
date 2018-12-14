namespace SonicRetro.KensSharp
{
    using System.Collections.Generic;
    using System.IO;

    public static partial class Saxman
    {
        private static void FindExtraMatches(byte[] data, long data_size, long offset, LZSS.NodeMeta[] node_meta_array)
        {
            // Find zero-fill matches
            if (offset < 0x1000)
            {
                for (long k = 0; k < 0xF + 3; ++k)
                {
                    if (data[offset + k] == 0)
                    {
                        long cost = GetMatchCost(0, k + 1);

                        if (cost != 0 && node_meta_array[offset + k + 1].cost > node_meta_array[offset].cost + cost)
                        {
                            node_meta_array[offset + k + 1].cost = node_meta_array[offset].cost + cost;
                            node_meta_array[offset + k + 1].previous_node_index = offset;
                            node_meta_array[offset + k + 1].match_length = k + 1;
                            node_meta_array[offset + k + 1].match_offset = 0xFFF;
                        }
                    }
                    else
                        break;
                }
            }
        }

        private static long GetMatchCost(long distance, long length)
        {
            if (length >= 3)
                return 1 + 16;
            else
                return 0;
        }

        private static void Encode(Stream input, Stream output, bool with_size)
        {
            long input_size = input.Length - input.Position;
            byte[] input_buffer = new byte[input_size];
            input.Read(input_buffer, 0, (int)input_size);

            long outputInitialPosition = output.Position;
            if (with_size)
            {
                output.Seek(2, SeekOrigin.Current);
            }

            LZSS.NodeMeta[] node_meta_array = LZSS.FindMatches(input_buffer, input_size, 0xF + 3, 0x1000, FindExtraMatches, 1 + 8, GetMatchCost);

            UInt8_NE_L_OutputBitStream bitStream = new UInt8_NE_L_OutputBitStream(output);
            MemoryStream data = new MemoryStream();

            for (long node_index = 0; node_meta_array[node_index].next_node_index != long.MaxValue; node_index = node_meta_array[node_index].next_node_index)
            {
                long next_index = node_meta_array[node_index].next_node_index;

                if (node_meta_array[next_index].match_length != 0)
                {
                    // Compressed
                    Push(bitStream, false, output, data);
                    long match_offset_adjusted = node_meta_array[next_index].match_offset - 0x12;   // I don't think there's any reason for this, the format's just stupid
                    NeutralEndian.Write1(data, (byte)(match_offset_adjusted & 0xFF));
                    NeutralEndian.Write1(data, (byte)(((match_offset_adjusted & 0xF00) >> 4) | ((node_meta_array[next_index].match_length - 3) & 0x0F)));
                }
                else
                {
                    // Uncompressed
                    Push(bitStream, true, output, data);
                    NeutralEndian.Write1(data, input_buffer[node_index]);
                }
            }

            // Write remaining data (normally we don't flush until we have a full descriptor byte)
            bitStream.Flush(true);
            byte[] dataArray = data.ToArray();
            output.Write(dataArray, 0, dataArray.Length);

            if (with_size)
            {
                ushort size = (ushort)(output.Position - 2);
                output.Seek(outputInitialPosition, SeekOrigin.Begin);
                LittleEndian.Write2(output, size);
            }
        }

        private static void Push(UInt8_NE_L_OutputBitStream bitStream, bool bit, Stream destination, MemoryStream data)
        {
            if (bitStream.Push(bit))
            {
                byte[] bytes = data.ToArray();
                destination.Write(bytes, 0, bytes.Length);
                data.SetLength(0);
            }
        }


        private static void Decode(Stream input, Stream output)
        {
            ushort size = LittleEndian.Read2(input);
            Decode(input, output, size);
        }

        private static void Decode(Stream input, Stream output, long size)
        {
            long end = input.Position + size;
            UInt8_NE_L_InputBitStream bitStream = new UInt8_NE_L_InputBitStream(input);
            List<byte> outputBuffer = new List<byte>();
            while (input.Position < end)
            {
                if (bitStream.Pop())
                {
                    if (input.Position >= end)
                    {
                        break;
                    }

                    outputBuffer.Add(NeutralEndian.Read1(input));
                }
                else
                {
                    if (input.Position >= end)
                    {
                        break;
                    }

                    int offset = NeutralEndian.Read1(input);

                    if (input.Position >= end)
                    {
                        break;
                    }

                    byte count = NeutralEndian.Read1(input);

                    // We've just read 2 bytes: %llllllll %hhhhcccc
                    // offset = %hhhhllllllll + 0x12, count = %cccc + 3
                    offset |= (ushort)((count & 0xF0) << 4);
                    offset += 0x12;
                    offset &= 0xFFF;
                    offset |= (ushort)(outputBuffer.Count & 0xF000);
                    count &= 0xF;
                    count += 3;

                    if (offset >= outputBuffer.Count)
                    {
                        offset -= 0x1000;
                    }

                    outputBuffer.AddRange(new byte[count]);

                    if (offset < 0)
                    {
                        // Zero-fill
                        for (int destinationIndex = outputBuffer.Count - count; destinationIndex < outputBuffer.Count; ++destinationIndex)
                        {
                            outputBuffer[destinationIndex] = 0;
                        }
                    }
                    else
                    {
                        // Dictionary reference
                        if (offset < outputBuffer.Count)
                        {
                            for (int sourceIndex = offset, destinationIndex = outputBuffer.Count - count;
                                destinationIndex < outputBuffer.Count;
                                sourceIndex++, destinationIndex++)
                            {
                                outputBuffer[destinationIndex] = outputBuffer[sourceIndex];
                            }
                        }
                    }
                }
            }

            byte[] bytes = outputBuffer.ToArray();
            output.Write(bytes, 0, bytes.Length);
        }
    }
}
