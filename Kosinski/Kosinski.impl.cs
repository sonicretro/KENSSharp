namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;
    using System.Security;

    public static partial class Kosinski
    {
        private const long SlidingWindow = 8192;
        private const long RecurrenceLength = 256;

        internal static void Encode(Stream source, Stream destination)
        {
            long size = source.Length - source.Position;
            byte[] buffer = new byte[size];
            source.Read(buffer, 0, (int)size);

            EncodeInternal(destination, buffer, 0, SlidingWindow, RecurrenceLength, size);
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
                EncodeInternal(destination, buffer, pos, SlidingWindow, RecurrenceLength, remainingSize);

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

        private static void EncodeInternal(Stream destination, byte[] buffer, long pos, long slidingWindow, long recLength, long size)
        {
            UInt16LEOutputBitStream bitStream = new UInt16LEOutputBitStream(destination, true);
            MemoryStream data = new MemoryStream();

            if (size > 0)
            {
                long bPointer = 1, iOffset = 0;
                bitStream.Push(true);
                NeutralEndian.Write1(data, buffer[pos]);

                while (bPointer < size)
                {
                    long iCount = Math.Min(recLength, size - bPointer);
                    long iMax = Math.Max(bPointer - slidingWindow, 0);
                    long k = 1;
                    long i = bPointer - 1;

                    do
                    {
                        long j = 0;
                        while (buffer[pos + i + j] == buffer[pos + bPointer + j])
                        {
                            if (++j >= iCount)
                            {
                                break;
                            }
                        }

                        if (j > k)
                        {
                            k = j;
                            iOffset = i;
                        }
                    } while (i-- > iMax);

                    iCount = k;

                    if (iCount == 1)
                    {
                        Push(bitStream, true, destination, data);
                        NeutralEndian.Write1(data, buffer[pos + bPointer]);
                    }
                    else if (iCount == 2 && bPointer - iOffset > 256)
                    {
                        Push(bitStream, true, destination, data);
                        NeutralEndian.Write1(data, buffer[pos + bPointer]);
                        --iCount;
                    }
                    else if (iCount < 6 && bPointer - iOffset <= 256)
                    {
                        Push(bitStream, false, destination, data);
                        Push(bitStream, false, destination, data);
                        Push(bitStream, (((iCount - 2) >> 1) & 1) != 0, destination, data);
                        Push(bitStream, ((iCount - 2) & 1) != 0, destination, data);
                        NeutralEndian.Write1(data, (byte)(~(bPointer - iOffset - 1)));
                    }
                    else
                    {
                        Push(bitStream, false, destination, data);
                        Push(bitStream, true, destination, data);

                        long off = bPointer - iOffset - 1;
                        ushort info = (ushort)(~((off << 8) | (off >> 5)) & 0xFFF8);

                        if (iCount < 10) // iCount - 2 < 8
                        {
                            info |= (ushort)(iCount - 2);
                            BigEndian.Write2(data, info);
                        }
                        else
                        {
                            BigEndian.Write2(data, info);
                            NeutralEndian.Write1(data, (byte)(iCount - 1));
                        }
                    }

                    bPointer += iCount;
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

        private static void Push(UInt16LEOutputBitStream bitStream, bool bit, Stream destination, MemoryStream data)
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
            UInt16LEInputBitStream bitStream = new UInt16LEInputBitStream(source, true, true);

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
