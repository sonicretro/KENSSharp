namespace SonicRetro.KensSharp
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static partial class Saxman
    {
        private static void Encode(Stream input, Stream output, bool withSize)
        {
            long inputSize = input.Length - input.Position + 0x12;
            byte[] inputBuffer = new byte[inputSize];
            input.Read(inputBuffer, 0x12, (int)(inputSize - 0x12));

            long outputInitialPosition = output.Position;
            if (withSize)
            {
                output.Seek(2, SeekOrigin.Current);
            }

            List<byte> data = new List<byte>();
            UInt8OutputBitStream bitStream = new UInt8OutputBitStream(output);

            long inputPointer = 0x12;
            while (inputPointer < inputSize)
            {
                // The maximum recurrence length that can be encoded is 0x12
                long count = Math.Min(inputSize - inputPointer, 0x12);

                // Minimal recurrence length, will contain the total recurrence length
                long offset = 0;
                long k = 1;
                long i = Math.Max(inputPointer - 0x1000, 0);

                do
                {
                    long j = 0;
                    while (inputBuffer[i + j] == inputBuffer[inputPointer + j] && ++j < count)
                    {
                    }

                    if (j > k)
                    {
                        k = j;
                        offset = i;
                    }

                    if (i == 0)
                    {
                        i = 11;
                    }
                } while (++i < inputPointer);

                count = k;

                if (count == 1 || count == 2)
                {
                    data.Add(inputBuffer[inputPointer]);
                    if (bitStream.Push(true))
                    {
                        byte[] dataArray = data.ToArray();
                        output.Write(dataArray, 0, dataArray.Length);
                        data.Clear();
                    }

                    count = 1;
                }
                else
                {
                    long iOffset = ((offset - 0x12) & 0xfff) - 0x12;
                    ushort word = (ushort)(((iOffset & 0xFF) << 8) | ((iOffset & 0xF00) >> 4) | ((count - 3) & 0x0F));
                    data.Add((byte)(word >> 8));
                    data.Add((byte)(word & 0xff));
                    if (bitStream.Push(false))
                    {
                        byte[] dataArray = data.ToArray();
                        output.Write(dataArray, 0, dataArray.Length);
                        data.Clear();
                    }
                }

                inputPointer += count;
            }

            {
                bitStream.Flush(true);
                byte[] dataArray = data.ToArray();
                output.Write(dataArray, 0, dataArray.Length);
                data.Clear();
            }

            if (withSize)
            {
                ushort size = (ushort)(output.Position - 2);
                output.Seek(outputInitialPosition, SeekOrigin.Begin);
                LittleEndian.Write2(output, size);
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
            UInt8InputBitStream bitStream = new UInt8InputBitStream(input);
            List<byte> outputBuffer = new List<byte>();
            while (input.Position < end)
            {
                if (bitStream.Unshift())
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

                    ushort offset = NeutralEndian.Read1(input);

                    if (input.Position >= end)
                    {
                        break;
                    }

                    byte count = NeutralEndian.Read1(input);

                    // We've just read 2 bytes: %llllllll %hhhhcccc
                    // offset = %hhhhllllllll + 0x12, count = %cccc + 3
                    offset |= (ushort)((count & 0xF0) << 4);
                    offset += 0x12;
                    offset |= (ushort)(outputBuffer.Count & 0xF000);
                    count &= 0xf;
                    count += 3;

                    if (offset >= outputBuffer.Count)
                    {
                        offset -= 0x1000;
                    }

                    outputBuffer.AddRange(new byte[count]);
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

            byte[] bytes = outputBuffer.ToArray();
            output.Write(bytes, 0, bytes.Length);
        }
    }
}
