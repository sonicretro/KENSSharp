namespace SonicRetro.KensSharp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public static partial class Nemesis
    {
#if false
        private static bool Encode(Stream input, Stream output)
        {
            throw new NotImplementedException();
        }
#endif

        private static bool Decode(Stream input, Stream output)
        {
            CodeTreeNode codeTree = new CodeTreeNode();
            ushort numberOfTiles = BigEndian.Read2(input);
            bool xorOutput = (numberOfTiles & 0x8000) != 0;
            numberOfTiles &= 0x7fff;
            DecodeHeader(input, output, codeTree);
            DecodeInternal(input, output, codeTree, numberOfTiles, xorOutput);
            return true;
        }

        private static void DecodeHeader(Stream input, Stream output, CodeTreeNode codeTree)
        {
            byte outputValue = 0;
            byte inputValue;

            // Loop until a byte with value 0xFF is encountered
            while ((inputValue = NeutralEndian.Read1(input)) != 0xFF)
            {
                if ((inputValue & 0x80) != 0)
                {
                    outputValue = (byte)(inputValue & 0xF);
                    inputValue = NeutralEndian.Read1(input);
                }

                codeTree.SetCode(
                    NeutralEndian.Read1(input),
                    inputValue & 0xF,
                    new NibbleRun(outputValue, (byte)(((inputValue & 0x70) >> 4) + 1)));
            }

            // Store a special nibble run for inline RLE sequences (code = 0b111111, length = 6)
            // Length = 0xFF in the nibble run is just a marker value that will be handled specially in DecodeInternal
            codeTree.SetCode(0x3F, 6, new NibbleRun(0, 0xFF));
        }

        private static void DecodeInternal(Stream input, Stream output, CodeTreeNode codeTree, ushort numberOfTiles, bool xorOutput)
        {
            UInt8InputBitStream inputBits = new UInt8InputBitStream(input);
            UInt8OutputBitStream outputBits;
            XorStream xorStream = null;
            try
            {
                if (xorOutput)
                {
                    xorStream = new XorStream(output);
                    outputBits = new UInt8OutputBitStream(xorStream);
                }
                else
                {
                    outputBits = new UInt8OutputBitStream(output);
                }

                // The output is: number of tiles * 0x20 (1 << 5) bytes per tile * 8 (1 << 3) bits per byte
                int outputSize = numberOfTiles << 8; // in bits
                int bitsWritten = 0;

                CodeTreeNode currentNode = codeTree;
                while (bitsWritten < outputSize)
                {
                    NibbleRun nibbleRun = currentNode.NibbleRun;
                    if (nibbleRun.Count == 0xFF)
                    {
                        // Bit pattern 0b111111; inline RLE.
                        // First 3 bits are repetition count, followed by the inlined nibble.
                        byte count = (byte)(inputBits.Read(3) + 1);
                        byte nibble = inputBits.Read(4);
                        DecodeNibbleRun(inputBits, outputBits, count, nibble, ref bitsWritten);
                        currentNode = codeTree;
                    }
                    else if (nibbleRun.Count != 0)
                    {
                        // Output the encoded nibble run
                        DecodeNibbleRun(inputBits, outputBits, nibbleRun.Count, nibbleRun.Nibble, ref bitsWritten);
                        currentNode = codeTree;
                    }
                    else
                    {
                        // Read the next bit and go down one level in the tree
                        currentNode = currentNode[inputBits.Get()];
                        if (currentNode == null)
                        {
                            throw new CompressionException(Properties.Resources.InvalidCode);
                        }
                    }
                }

                outputBits.Flush(false);
            }
            finally
            {
                if (xorStream != null)
                {
                    xorStream.Dispose();
                }
            }
        }

        private static void DecodeNibbleRun(UInt8InputBitStream inputBits, UInt8OutputBitStream outputBits, byte count, byte nibble, ref int bitsWritten)
        {
            bitsWritten += count * 4;

            // Write single nibble, if needed
            if ((count & 1) != 0)
            {
                outputBits.Write(nibble, 4);
            }

            // Write pairs of nibbles
            count >>= 1;
            nibble |= (byte)(nibble << 4);
            while (count-- != 0)
            {
                outputBits.Write(nibble, 8);
            }
        }

        private struct NibbleRun
        {
            public byte Nibble;
            public byte Count;

            public NibbleRun(byte nibble, byte count)
                : this()
            {
                this.Nibble = nibble;
                this.Count = count;
            }

            public override string ToString()
            {
                return this.Count.ToString() + " × " + this.Nibble.ToString("X");
            }
        }

        private sealed class CodeTreeNode
        {
            private CodeTreeNode clear;
            private CodeTreeNode set;
            private NibbleRun nibbleRun;

            public void SetCode(byte code, int length, NibbleRun nibbleRun)
            {
                if (length == 0)
                {
                    if (this.clear != null || this.set != null)
                    {
                        throw new CompressionException(Properties.Resources.CodeAlreadyUsedAsPrefix);
                    }

                    this.nibbleRun = nibbleRun;
                }
                else
                {
                    if (this.nibbleRun.Count != 0)
                    {
                        throw new CompressionException(Properties.Resources.PrefixAlreadyUsedAsCode);
                    }

                    --length;
                    if ((code & (1 << length)) == 0)
                    {
                        if (this.clear == null)
                        {
                            this.clear = new CodeTreeNode();
                        }

                        this.clear.SetCode(code, length, nibbleRun);
                    }
                    else
                    {
                        if (this.set == null)
                        {
                            this.set = new CodeTreeNode();
                        }

                        this.set.SetCode((byte)(code & ((1 << length) - 1)), length, nibbleRun);
                    }
                }
            }

            public CodeTreeNode this[bool side]
            {
                get
                {
                    return side ? this.set : this.clear;
                }
            }

            public NibbleRun NibbleRun
            {
                get
                {
                    return this.nibbleRun;
                }
            }
        }

        private sealed class XorStream : Stream
        {
            private Stream stream;
            private int subPosition; // 0-3
            private byte[] bytes;

            public XorStream(Stream stream)
            {
                if (stream == null)
                {
                    throw new ArgumentNullException("stream");
                }

                this.stream = stream;

                // We pass an array to stream.Write, so why not create one now and reuse it?
                this.bytes = new byte[4];
            }

            public override bool CanRead
            {
                get { throw new System.NotImplementedException(); }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override void Flush()
            {
                this.stream.Flush();
            }

            public override long Length
            {
                get { return this.stream.Length; }
            }

            public override long Position
            {
                get
                {
                    return this.stream.Position;
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException("buffer");
                }

                if (offset < 0)
                {
                    throw new ArgumentException(Properties.Resources.NegativeOffset, "offset");
                }

                if (count < 0)
                {
                    throw new ArgumentException(Properties.Resources.NegativeCount, "count");
                }

                if (offset > buffer.Length)
                {
                    throw new ArgumentException(Properties.Resources.OffsetIsGreaterThanBufferSize, "offset");
                }

                if (offset + count > buffer.Length)
                {
                    throw new ArgumentException(Properties.Resources.OffsetPlusCountIsGreaterThanBufferSize);
                }

                if (count == 0)
                {
                    return;
                }

                int index = 0;
                switch (this.subPosition)
                {
                    case 1:
                        this.bytes[1] ^= buffer[offset + index++];
                        if (index >= count)
                        {
                            this.subPosition = 2;
                            return;
                        }

                        goto case 2;

                    case 2:
                        this.bytes[2] ^= buffer[offset + index++];
                        if (index >= count)
                        {
                            this.subPosition = 3;
                            return;
                        }

                        goto case 3;

                    case 3:
                        this.bytes[3] ^= buffer[offset + index++];
                        this.subPosition = 0;
                        this.WriteBytes();
                        break;
                }

                Debug.Assert(this.subPosition == 0, "XorStream.subPosition should be 0.");
                while (count - index >= 4)
                {
                    this.bytes[0] ^= buffer[offset + index++];
                    this.bytes[1] ^= buffer[offset + index++];
                    this.bytes[2] ^= buffer[offset + index++];
                    this.bytes[3] ^= buffer[offset + index++];
                    this.WriteBytes();
                }

                if (index < count)
                {
                    this.bytes[0] ^= buffer[offset + index++];
                    ++this.subPosition;
                    if (index < count)
                    {
                        this.bytes[1] ^= buffer[offset + index++];
                        ++this.subPosition;
                        if (index < count)
                        {
                            this.bytes[2] ^= buffer[offset + index++];
                            ++this.subPosition;
                        }
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (this.subPosition > 0)
                {
                    // Write the pending bytes to the stream
                    this.stream.Write(this.bytes, 0, this.subPosition);
                }

                base.Dispose(disposing);
            }

            private void WriteBytes()
            {
                this.stream.Write(this.bytes, 0, 4);
            }
        }
    }
}
