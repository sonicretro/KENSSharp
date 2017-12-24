// 16-bit big-endian output bitstream
// Data is stored early (right after the last bit is pushed, rather than before a new bit is pushed)
// Bits are pushed lowest-first

namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public sealed class UInt16BEOutputBitStream : OutputBitStream<ushort>
    {
        private Stream stream;
        private int waitingBits;
        private ushort byteBuffer;

        public UInt16BEOutputBitStream(Stream stream)
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
            this.byteBuffer |= Convert.ToUInt16(bit);
            if (++this.waitingBits >= 16)
            {
                BigEndian.Write2(this.stream, this.byteBuffer);
                this.waitingBits = 0;
                this.byteBuffer = 0;
                return true;
            }

            return false;
        }

        public override bool Push(bool bit)
        {
            this.byteBuffer |= (ushort)(Convert.ToUInt16(bit) << this.waitingBits);

            if (++this.waitingBits >= 16)
            {
                BigEndian.Write2(this.stream, this.byteBuffer);
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
                    this.byteBuffer <<= 16 - this.waitingBits;
                }

                BigEndian.Write2(this.stream, this.byteBuffer);
                this.waitingBits = 0;
                return true;
            }

            return false;
        }

        public override bool Write(ushort data, int size)
        {
            if (this.waitingBits + size >= 16)
            {
                int delta = 16 - this.waitingBits;
                this.waitingBits = (this.waitingBits + size) % 16;
                ushort bits = (ushort)((this.byteBuffer << delta) | (data >> this.waitingBits));
                BigEndian.Write2(this.stream, bits);
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
