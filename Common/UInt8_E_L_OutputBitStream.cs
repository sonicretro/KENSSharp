// 8-bit output bitstream
// Data is stored early (right after the last bit is pushed, rather than before a new bit is pushed)
// Bits are pushed lowest-first

namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public sealed class UInt8_E_L_OutputBitStream : OutputBitStream<byte>
    {
        private Stream stream;
        private int waitingBits;
        private byte byteBuffer;

        public UInt8_E_L_OutputBitStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.stream = stream;
        }

        public override bool Put(bool bit)
        {
            this.byteBuffer <<= 1;
            this.byteBuffer |= Convert.ToByte(bit);
            if (++this.waitingBits >= 8)
            {
                NeutralEndian.Write1(this.stream, this.byteBuffer);
                this.waitingBits = 0;
                this.byteBuffer = 0;
                return true;
            }

            return false;
        }

        public override bool Push(bool bit)
        {
            this.byteBuffer |= (byte)(Convert.ToByte(bit) << this.waitingBits);
            if (++this.waitingBits >= 8)
            {
                NeutralEndian.Write1(this.stream, this.byteBuffer);
                this.waitingBits = 0;
                this.byteBuffer = 0;
                return true;
            }

            return false;
        }

        public override bool Flush(bool unchanged)
        {
            if (this.waitingBits != 0)
            {
                if (!unchanged)
                {
                    this.byteBuffer <<= 8 - this.waitingBits;
                }

                NeutralEndian.Write1(this.stream, this.byteBuffer);
                this.waitingBits = 0;
                return true;
            }

            return false;
        }

        public override bool Write(byte data, int size)
        {
            if (this.waitingBits + size >= 8)
            {
                int delta = 8 - this.waitingBits;
                this.waitingBits = (this.waitingBits + size) % 8;
                byte bits = (byte)((this.byteBuffer << delta) | (data >> this.waitingBits));
                NeutralEndian.Write1(this.stream, bits);
                this.byteBuffer = data;
                return true;
            }

            this.byteBuffer <<= size;
            this.byteBuffer |= data;
            this.waitingBits += size;
            return false;
        }
    }
}
