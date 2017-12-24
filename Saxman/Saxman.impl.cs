namespace SonicRetro.KensSharp
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static partial class Saxman
    {
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

            List<byte> data = new List<byte>();
            UInt8_NE_L_OutputBitStream bitStream = new UInt8_NE_L_OutputBitStream(output);

            long input_pointer = 0;
            while (input_pointer < input_size)
            {
                // The maximum recurrence length that can be encoded is 0x12
                // Of course, if the remaining file is smaller, cap to that instead
                long maximum_match_length = Math.Min(input_size - input_pointer, 0x12);
                // The furthest back Saxman can address is 0x1000 bytes
                // Again, if there's less than 0x1000 bytes of data available, then cap at that instead
                long maximum_backsearch = Math.Min(input_pointer, 0x1000);

                // These are our default values for the longest match found
                long longest_match_offset = input_pointer;	// This one doesn't really need initialising, but it does shut up some moronic warnings
                long longest_match_length = 1;

                // First, look for dictionary matches
                for (long backsearch_pointer = input_pointer - 1; backsearch_pointer >= input_pointer - maximum_backsearch; --backsearch_pointer)
                {
                    long match_length = 0;
                    while (input_buffer[backsearch_pointer + match_length] == input_buffer[input_pointer + match_length] && ++match_length < maximum_match_length) ;

                    if (match_length > longest_match_length)
                    {
                        longest_match_length = match_length;
                        longest_match_offset = backsearch_pointer;
                    }
                }

                // Then, look for zero-fill matches
                if (input_pointer < 0xFFF)  // Saxman cannot perform zero-fills past the first 0xFFF bytes (it relies on some goofy logic in the decompressor)
                {
                    long match_length = 0;
                    while (input_buffer[input_pointer + match_length] == 0 && ++match_length < maximum_match_length) ;

                    if (match_length > longest_match_length)
                    {
                        longest_match_length = match_length;
                        // Saxman detects zero-fills by checking if the dictionary reference offset is somehow
                        // pointing to *after* the decompressed data, so we set it to the highest possible value here
                        longest_match_offset = 0xFFF;
                    }
                }

                // We cannot compress runs shorter than three bytes
                if (longest_match_length < 3)
                {
                    // Uncompressed
                    Push(bitStream, true, output, data);
                    data.Add(input_buffer[input_pointer]);

                    longest_match_length = 1;
                }
                else
                {
                    // Compressed
                    Push(bitStream, false, output, data);
                    long match_offset_adjusted = longest_match_offset - 0x12;   // I don't think there's any reason for this, the format's just stupid
                    data.Add((byte)(match_offset_adjusted & 0xFF));
                    data.Add((byte)(((match_offset_adjusted & 0xF00) >> 4) | ((longest_match_length - 3) & 0x0F)));
                }

                input_pointer += longest_match_length;
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

        private static void Push(UInt8_NE_L_OutputBitStream bitStream, bool bit, Stream destination, List<byte> data)
        {
            if (bitStream.Push(bit))
            {
                byte[] bytes = data.ToArray();
                destination.Write(bytes, 0, bytes.Length);
                data.Clear();
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
