namespace SonicRetro.KensSharp
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;

    public static partial class Enigma
    {
        // Cached delegates
        private static readonly Action<Stream, ushort> Write2BE = BigEndian.Write2;
        private static readonly Action<Stream, ushort> Write2LE = LittleEndian.Write2;

        private static readonly MethodInfo UShortInputBitStreamGet = typeof(InputBitStream<ushort>).GetMethod("Get");
        private static readonly MethodInfo UShortOutputBitStreamPut = typeof(OutputBitStream<ushort>).GetMethod("Put");
        private static readonly Func<InputBitStream<ushort>, ushort>[] BitfieldReaders =
            new Func<InputBitStream<ushort>, ushort>[0x20];
        private static readonly Action<OutputBitStream<ushort>, ushort>[] BitfieldWriters =
            new Action<OutputBitStream<ushort>, ushort>[0x20];

        private static void Encode(Stream input, Stream output, Endianness endianness)
        {
            Action<Stream, ushort> write2;
            OutputBitStream<ushort> bitStream;
            if (endianness == Endianness.BigEndian)
            {
                write2 = Write2BE;
                bitStream = new UInt16BEOutputBitStream(output);
            }
            else
            {
                write2 = Write2LE;
                bitStream = new UInt16LEOutputBitStream(output);
            }

            // To unpack source into 2-byte words.
            ushort[] words = new ushort[(input.Length - input.Position) / 2];
            if (words.Length == 0)
            {
                throw new CompressionException(Properties.Resources.EmptySource);
            }

            // Frequency map.
            SortedList<ushort, long> counts = new SortedList<ushort, long>();

            // Presence map.
            HashSet<ushort> elements = new HashSet<ushort>();

            // Unpack source into array. Along the way, build frequency and presence maps.
            ushort maskValue = 0;

            {
                byte[] buffer = new byte[2];
                int i = 0, bytesRead;
                while ((bytesRead = input.Read(buffer, 0, 2)) == 2)
                {
                    ushort v = (ushort)(buffer[0] << 8 | buffer[1]);
                    maskValue |= v;
                    long count;
                    counts.TryGetValue(v, out count);
                    counts[v] = count + 1;
                    elements.Add(v);
                    words[i++] = v;
                }
            }

            var writeBitfield = GetBitfieldWriter((byte)(maskValue >> 11));
            byte packetLength = (byte)(Log2((ushort)(maskValue & 0x7ff)) + 1);

            // Find the most common 2-byte value.
            ushort commonValue = FindMostFrequentWord(counts);

            // Find incrementing (not necessarily contiguous) runs.
            // The original algorithm does this for all 65536 2-byte words, while
            // this version only checks the 2-byte words actually in the file.
            SortedList<ushort, long> runs = new SortedList<ushort, long>();
            foreach (ushort element in elements)
            {
                ushort next = element;
                long runLength = 0;
                foreach (ushort word in words)
                {
                    if (word == next)
                    {
                        ++next;
                        ++runLength;
                    }
                }

                runs[element] = runLength;
            }

            // Find the starting 2-byte value with the longest incrementing run.
            ushort incrementingValue = FindMostFrequentWord(runs);

            // Output header.
            NeutralEndian.Write1(output, packetLength);
            NeutralEndian.Write1(output, (byte)(maskValue >> 11));
            write2(output, incrementingValue);
            write2(output, commonValue);

            // Output compressed data.
            List<ushort> buf = new List<ushort>();
            int pos = 0;
            while (pos < words.Length)
            {
                ushort v = words[pos];
                if (v == incrementingValue)
                {
                    FlushBuffer(buf, bitStream, writeBitfield, packetLength);
                    ushort next = (ushort)(v + 1);
                    ushort count = 0;
                    for (int i = pos + 1; i < words.Length && count < 0xf; i++)
                    {
                        if (next != words[i])
                        {
                            break;
                        }

                        ++next;
                        ++count;
                    }

                    bitStream.Write((ushort)(0x00 | count), 6);
                    incrementingValue = next;
                    pos += count;
                }
                else if (v == commonValue)
                {
                    FlushBuffer(buf, bitStream, writeBitfield, packetLength);
                    ushort count = 0;
                    for (int i = pos + 1; i < words.Length && count < 0xf; i++)
                    {
                        if (v != words[i])
                        {
                            break;
                        }

                        ++count;
                    }

                    bitStream.Write((ushort)(0x10 | count), 6);
                    pos += count;
                }
                else
                {
                    ushort next;
                    int delta;
                    if (pos + 1 < words.Length &&
                        (next = words[pos + 1]) != incrementingValue &&
                        ((delta = (int)next - (int)v) == -1 || delta == 0 || delta == 1))
                    {
                        FlushBuffer(buf, bitStream, writeBitfield, packetLength);
                        ushort count = 1;
                        next = (ushort)(next + delta);
                        for (int i = pos + 2; i < words.Length && count < 0xf; i++)
                        {
                            if (next != words[i])
                            {
                                break;
                            }

                            // If the word is equal to the incrementing word value, stop this run early so we can use the
                            // incrementing value in the next iteration of the main loop.
                            if (words[i] == incrementingValue)
                            {
                                break;
                            }

                            next = (ushort)(next + delta);
                            ++count;
                        }

                        if (delta == -1)
                        {
                            delta = 2;
                        }

                        delta |= 4;
                        delta <<= 4;
                        bitStream.Write((ushort)(delta | count), 7);
                        writeBitfield(bitStream, v);
                        bitStream.Write((ushort)(v & 0x7ff), packetLength);
                        pos += count;
                    }
                    else
                    {
                        if (buf.Count >= 0xf)
                        {
                            FlushBuffer(buf, bitStream, writeBitfield, packetLength);
                        }

                        buf.Add(v);
                    }
                }

                ++pos;
            }

            FlushBuffer(buf, bitStream, writeBitfield, packetLength);

            // Terminator
            bitStream.Write(0x7f, 7);
            bitStream.Flush(false);
        }

        private static void Decode(Stream input, Stream output, Endianness endianness)
        {
            using (PaddedStream paddedInput = new PaddedStream(input, 2, PaddedStreamMode.Read))
            {
                byte packetLength = NeutralEndian.Read1(paddedInput);
                var readBitfield = GetBitfieldReader(NeutralEndian.Read1(paddedInput));

                ushort incrementingValue;
                ushort commonValue;
                InputBitStream<ushort> bitStream;
                Action<Stream, ushort> write2;

                if (endianness == Endianness.BigEndian)
                {
                    incrementingValue = BigEndian.Read2(paddedInput);
                    commonValue = BigEndian.Read2(paddedInput);
                    bitStream = new UInt16BEInputBitStream(paddedInput);
                    write2 = Write2BE;
                }
                else
                {
                    incrementingValue = LittleEndian.Read2(paddedInput);
                    commonValue = LittleEndian.Read2(paddedInput);
                    bitStream = new UInt16LEInputBitStream(paddedInput);
                    write2 = Write2LE;
                }

                // Loop until the end-of-data marker is found (if it is not found before the end of the stream, UInt8InputBitStream
                // will throw an exception)
                for (; ; )
                {
                    if (bitStream.Get())
                    {
                        int mode = bitStream.Read(2);
                        int count = bitStream.Read(4);
                        switch (mode)
                        {
                            case 0:
                            case 1:
                                {
                                    ushort flags = readBitfield(bitStream);
                                    ushort outv = (ushort)(bitStream.Read(packetLength) | flags);

                                    do
                                    {
                                        write2(output, outv);
                                        outv += (ushort)mode;
                                    } while (--count >= 0);
                                }

                                break;

                            case 2:
                                mode = -1;
                                goto case 0;

                            case 3:
                                {
                                    // End of compressed data
                                    if (count == 0xf)
                                    {
                                        return;
                                    }

                                    do
                                    {
                                        ushort flags = readBitfield(bitStream);
                                        ushort outv = bitStream.Read(packetLength);
                                        write2(output, (ushort)(outv | flags));
                                    } while (--count >= 0);
                                }

                                break;
                        }
                    }
                    else
                    {
                        bool mode = bitStream.Get();
                        int count = bitStream.Read(4);
                        if (mode)
                        {
                            do
                            {
                                write2(output, commonValue);
                            } while (--count >= 0);
                        }
                        else
                        {
                            do
                            {
                                write2(output, incrementingValue++);
                            } while (--count >= 0);
                        }
                    }
                }
            }
        }

        private static int Log2(ushort value)
        {
            int result;
            int shift;

            // Set bit 3 of 'result' if 'value' has at least one of bits 8-15 set
            result = Convert.ToInt32((value & ~0xff) != 0) << 3; // either 0 or 8
            value >>= result; // shift value right by 8 if at least one of the bits is set

            // Set bit 2 of 'result' if 'value' has at least one of bits 4-7 set
            shift = Convert.ToInt32((value & ~0xf) != 0) << 2; // either 0 or 4
            result |= shift;
            value >>= shift;

            // Set bit 1 of 'result' if 'value' has bit 2 or bit 3 set
            shift = Convert.ToInt32((value & ~0x3) != 0) << 1; // either 0 or 2
            result |= shift;
            value >>= shift;

            // Set bit 0 of 'result' if 'value' has bit 1 set
            result |= value >> 1; // either 0 or 1
            return result;
        }

        private static ushort FindMostFrequentWord(SortedList<ushort, long> counts)
        {
            ushort mostFrequentWord = 0;
            long largestCountSoFar = 0;
            foreach (var item in counts)
            {
                if (item.Value > largestCountSoFar)
                {
                    mostFrequentWord = item.Key;
                    largestCountSoFar = item.Value;
                }
            }

            return mostFrequentWord;
        }

        private static void FlushBuffer(List<ushort> buf, OutputBitStream<ushort> bitStream, Action<OutputBitStream<ushort>, ushort> writeBitfield, byte packetLength)
        {
            if (buf.Count == 0)
            {
                return;
            }

            bitStream.Write((ushort)(0x70 | ((buf.Count - 1) & 0xf)), 7);
            foreach (ushort word in buf)
            {
                writeBitfield(bitStream, word);
                bitStream.Write((ushort)(word & 0x7ff), packetLength);
            }

            buf.Clear();
        }

        private static Func<InputBitStream<ushort>, ushort> GetBitfieldReader(byte enabledFlags)
        {
            if (enabledFlags > 0x1f)
            {
                throw new ArgumentOutOfRangeException("enabledFlags");
            }

            // Lazily initialize the bitfield readers
            if (BitfieldReaders[enabledFlags] == null)
            {
                // Lock the array for thread safety
                lock (BitfieldReaders)
                {
                    // If the bitfield reader has been compiled while we were waiting for the lock, don't initialize it again!
                    if (BitfieldReaders[enabledFlags] == null)
                    {
                        DynamicMethod method = new DynamicMethod(
                            string.Format(CultureInfo.InvariantCulture, "ReadBitfield<{0}>", enabledFlags),
                            typeof(ushort),
                            new Type[] { typeof(InputBitStream<ushort>) });
                        method.DefineParameter(0, ParameterAttributes.None, "bitStream");
                        ILGenerator ilg = method.GetILGenerator();
                        if (enabledFlags == 0)
                        {
                            ilg.Emit(OpCodes.Ldc_I4_0);
                        }
                        else
                        {
                            // Keep track of how many bits are set, so we can generate the correct number of 'or' instructions
                            int bits = 0;

                            // Loop through the bits
                            for (int i = 4; i >= 0; i--)
                            {
                                // Test bits 4 to 0
                                if ((enabledFlags & (1 << i)) != 0)
                                {
                                    ++bits;

                                    // Call InputBitStream<ushort>.Get()
                                    ilg.Emit(OpCodes.Ldarg_0); // bitStream
                                    ilg.Emit(OpCodes.Callvirt, UShortInputBitStreamGet);

                                    // Shift the value by 15 to 11, depending on the bit being tested
                                    ilg.Emit(OpCodes.Ldc_I4_S, 11 + i);
                                    ilg.Emit(OpCodes.Shl);
                                }
                            }

                            // Decrement by one to keep one value on the stack
                            // For example, if enabledFlags is 0x1f, there will be 5 values on the stack. We want to end up with
                            // one value on the stack, so we must emit 4 'or' instructions.
                            --bits;
                            for (int i = 0; i < bits; i++)
                            {
                                // Emit 'or' instructions
                                ilg.Emit(OpCodes.Or);
                            }
                        }

                        ilg.Emit(OpCodes.Ret);

                        BitfieldReaders[enabledFlags] = (Func<InputBitStream<ushort>, ushort>)method.CreateDelegate(
                            typeof(Func<InputBitStream<ushort>, ushort>));
                    }
                }
            }

            return BitfieldReaders[enabledFlags];
        }

        private static Action<OutputBitStream<ushort>, ushort> GetBitfieldWriter(byte enabledFlags)
        {
            if (enabledFlags > 0x1f)
            {
                throw new ArgumentOutOfRangeException("enabledFlags");
            }

            // Lazily initialize the bitfield readers
            if (BitfieldWriters[enabledFlags] == null)
            {
                // Lock the array for thread safety
                lock (BitfieldWriters)
                {
                    // If the bitfield reader has been compiled while we were waiting for the lock, don't initialize it again!
                    if (BitfieldWriters[enabledFlags] == null)
                    {
                        DynamicMethod method = new DynamicMethod(
                            string.Format(CultureInfo.InvariantCulture, "WriteBitfield<{0}>", enabledFlags),
                            null,
                            new Type[] { typeof(OutputBitStream<ushort>), typeof(ushort) });
                        method.DefineParameter(0, ParameterAttributes.None, "bitStream");
                        method.DefineParameter(1, ParameterAttributes.None, "flags");
                        ILGenerator ilg = method.GetILGenerator();

                        // Loop through the bits
                        for (int i = 4; i >= 0; i--)
                        {
                            // Test bits 4 to 0
                            if ((enabledFlags & (1 << i)) != 0)
                            {
                                // Load 'bitStream' for the call to OutputBitStream<ushort>.Put()
                                ilg.Emit(OpCodes.Ldarg_0); // bitStream

                                // Load 'flags', shift the value by 15 to 11, depending on the bit being tested
                                ilg.Emit(OpCodes.Ldarg_1); // flags
                                ilg.Emit(OpCodes.Ldc_I4_S, 11 + i);
                                ilg.Emit(OpCodes.Shr);

                                // Call OutputBitStream<ushort>.Put()
                                ilg.Emit(OpCodes.Callvirt, UShortOutputBitStreamPut);

                                // Pop return value of OutputBitStream<ushort>.Put() from the stack
                                ilg.Emit(OpCodes.Pop);
                            }
                        }

                        ilg.Emit(OpCodes.Ret);

                        BitfieldWriters[enabledFlags] = (Action<OutputBitStream<ushort>, ushort>)method.CreateDelegate(
                            typeof(Action<OutputBitStream<ushort>, ushort>));
                    }
                }
            }

            return BitfieldWriters[enabledFlags];
        }
    }
}
