namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;
    using System.Security;

    public static partial class Comper
    {
        private const long SlidingWindow = 256 * 2;         // *2 because Comper counts in words (16-bits)
        private const long RecurrenceLength = 256 * 2;

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

        [SecurityCritical]
        private static unsafe void EncodeInternal(Stream destination, byte* buffer, long slidingWindow, long recLength, long size)
        {
            UInt16BEOutputBitStream bitStream = new UInt16BEOutputBitStream(destination);
            bitStream.littleEndianBits = false;
            MemoryStream data = new MemoryStream();

            if (size > 0)
            {
                long bPointer = 2, iOffset = 0;
                // Write initial "match" (always a symbol)
                bitStream.Push(false);
                NeutralEndian.Write1(data, buffer[0]);
                NeutralEndian.Write1(data, buffer[1]);

                while (bPointer < size)
                {
                    long iCount = Math.Min(recLength, size - bPointer);
                    long iMax = Math.Max(bPointer - slidingWindow, 0);
                    long longestMatch = 2;
                    long backSearch = bPointer;

                    do
                    {
                        backSearch -= 2;
                        long currentCount = 0;
                        while (buffer[backSearch + currentCount] == buffer[bPointer + currentCount] && buffer[backSearch + currentCount + 1] == buffer[bPointer + currentCount + 1])
                        {
                            currentCount += 2;
                            if (currentCount >= iCount)
                            {
                                // Match is as big as the look-forward buffer (or file) will let it be
                                break;
                            }
                        }

                        if (currentCount > longestMatch)
                        {
                            longestMatch = currentCount;
                            iOffset = backSearch;
                        }
                    } while (backSearch > iMax);    // Repeat for as far back as search buffer will let us

                    iCount = longestMatch/2;    // Comper counts in words (16 bits)

                    if (iCount == 1)
                    {
                        //Push(bitStream, false, destination, data);    // Early descriptor (as seen in Kosinski)
                        NeutralEndian.Write1(data, buffer[bPointer]);
                        NeutralEndian.Write1(data, buffer[bPointer + 1]);
                        Push(bitStream, false, destination, data);  // Non-early descriptor
                    }
                    else
                    {
                        //Push(bitStream, true, destination, data); // Early descriptor (as seen in Kosinski)
                        NeutralEndian.Write1(data, (byte)((iOffset - bPointer)/2)); // Comper's offsets count in words (16-bits)
                        NeutralEndian.Write1(data, (byte)(iCount - 1));
                        Push(bitStream, true, destination, data);   // Non-early descriptor
                    }

                    bPointer += iCount*2;   // iCount counts in words (16-bits), so we correct it to bytes (8-bits) here
                }
            }
            
            Push(bitStream, true, destination, data);

            // Early descriptor only
            // If the bit stream was just flushed, write an empty bit stream that will be read just before the end-of-data
            // sequence below.
            /*if (!bitStream.HasWaitingBits)
            {
                NeutralEndian.Write1(data, 0);
                NeutralEndian.Write1(data, 0);
            }*/

            NeutralEndian.Write1(data, 0);
            NeutralEndian.Write1(data, 0);
            bitStream.Flush(true);

            byte[] bytes = data.ToArray();
            destination.Write(bytes, 0, bytes.Length);
        }

        private static void Push(UInt16BEOutputBitStream bitStream, bool bit, Stream destination, MemoryStream data)
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
            UInt16BEInputBitStream bitStream = new UInt16BEInputBitStream(source);
			bitStream.littleEndianBits = false;
			bitStream.earlyDescriptor = false;


			for (;;)
            {
                if (!bitStream.Pop())
                {
					ushort word = BigEndian.Read2(source);

					BigEndian.Write2(destination, word);
                }
                else
                {
                    int distance = (0x100 - NeutralEndian.Read1(source)) * 2;
					int length = NeutralEndian.Read1(source);
					if (length == 0)
                    {
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
