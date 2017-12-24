// 8-bit output bitstream
// Data is not stored early (right before a new bit is pushed, rather than after the last bit is pushed)
// Bits are pushed highest-first

namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public sealed class UInt8_NE_H_OutputBitStream : OutputBitStream<byte>
    {
        private Stream stream;
        private int waitingBits;
        private byte byteBuffer;

        public UInt8_NE_H_OutputBitStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.stream = stream;
        }

        public override bool Put(bool bit)
        {
            bool flushed = false;

            if (this.waitingBits >= 8)
            {
                NeutralEndian.Write1(this.stream, this.byteBuffer);
                this.waitingBits = 0;
                this.byteBuffer = 0;
                flushed = true;
            }

            this.byteBuffer >>= 1;

            if (bit)
                this.byteBuffer |= 0x80;

            ++this.waitingBits;

            return flushed;
        }

        public override bool Push(bool bit)
        {
            bool flushed = false;

            if (this.waitingBits >= 8)
            {
                NeutralEndian.Write1(this.stream, this.byteBuffer);
                this.waitingBits = 0;
                this.byteBuffer = 0;
                flushed = true;
            }

            if (bit)
                this.byteBuffer |= (byte)(0x80 >> this.waitingBits);

            ++this.waitingBits;

            return flushed;
        }

        public override bool Flush(bool unchanged)
        {
            if (this.waitingBits != 0)
            {
                if (!unchanged)
                {
                    this.byteBuffer >>= 8 - this.waitingBits;
                }

                NeutralEndian.Write1(this.stream, this.byteBuffer);
                this.waitingBits = 0;
                return true;
            }

            return false;
        }

        public override bool Write(byte data, int size)
        {
            // Not implemented
            return false;
        }
    }
}
