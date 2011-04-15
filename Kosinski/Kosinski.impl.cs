namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;
    using System.Security;

    public static partial class Kosinski
    {
        private const long SlidingWindow = 8192;
        private const long RecurrenceLength = 256;

        [SecuritySafeCritical]
        internal static unsafe void Encode(Stream source, Stream destination)
        {
            long size = source.Length - source.Position;
            byte[] buffer = new byte[size];
            source.Read(buffer, 0, (int)size);

            fixed (byte* ptr = buffer)
            {
                EncodeInternal(destination, ptr, SlidingWindow, RecurrenceLength, size);
            }
        }

        [SecuritySafeCritical]
        internal static unsafe void Encode(Stream source, Stream destination, Endianness headerEndianness)
        {
            long size = source.Length - source.Position;
            byte[] buffer = new byte[size];
            source.Read(buffer, 0, (int)size);

            fixed (byte* fixedPtr = buffer)
            {
                byte* ptr = fixedPtr;
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
                    EncodeInternal(destination, ptr, SlidingWindow, RecurrenceLength, remainingSize);

                    compBytes += remainingSize;
                    ptr += remainingSize;

                    if (compBytes >= size)
                    {
                        break;
                    }

                    // Padding between modules
                    int n = (int)(16 - (destination.Position - 2) % 16);
                    for (int i = 0; i < n; i++)
                    {
                        destination.WriteByte(0);
                    }

                    remainingSize = Math.Min(0x1000L, size - compBytes);
                }
            }
        }

        [SecurityCritical]
        private static unsafe void EncodeInternal(Stream destination, byte* buffer, long slidingWindow, long recLength, long size)
        {
            UInt16LEOutputBitStream bitStream = new UInt16LEOutputBitStream(destination);
            MemoryStream data = new MemoryStream();
            long bPointer = 1, iOffset = 0;
            bitStream.Push(true);
            NeutralEndian.Write1(data, buffer[0]);

            do
            {
                long iCount = Math.Min(recLength, size - bPointer);
                long iMax = Math.Max(bPointer - slidingWindow, 0);
                long k = 1;
                long i = bPointer - 1;

                do
                {
                    long j = 0;
                    while (buffer[i + j] == buffer[bPointer + j])
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
                    NeutralEndian.Write1(data, buffer[bPointer]);
                }
                else if (iCount == 2 && bPointer - iOffset > 256)
                {
                    Push(bitStream, true, destination, data);
                    NeutralEndian.Write1(data, buffer[bPointer]);
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
            } while (bPointer < size);

            Push(bitStream, false, destination, data);
            Push(bitStream, true, destination, data);

            // If the bit stream was just flushed, write an empty bit stream that will be read just before the end-of-data
            // sequence below.
            if (data.Length == 0)
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

                // Skip null bytes
                int b;
                while ((b = source.ReadByte()) == 0)
                {
                }

                if (b == -1)
                {
                    throw new EndOfStreamException();
                }

                // Position the stream back on the null
                source.Seek(-1, SeekOrigin.Current);
            }
        }

        private static void DecodeInternal(Stream source, Stream destination, ref long decompressedBytes)
        {
            UInt16LEInputBitStream bitStream = new UInt16LEInputBitStream(source);

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
