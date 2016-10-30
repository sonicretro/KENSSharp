namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public sealed class UInt8OutputBitStream : OutputBitStream<byte>
    {
        private Stream stream;
        private int waitingBits;
        private byte byteBuffer;
        private bool littleEndianBits;

        public UInt8OutputBitStream(Stream stream, bool littleEndianBits)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.stream = stream;
            this.littleEndianBits = littleEndianBits;
        }

        public override bool Put(bool bit)
        {
            this.byteBuffer <<= 1;
            this.byteBuffer |= Convert.ToByte(bit);
            if (++this.waitingBits >= 8)
            {
                NeutralEndian.Write1(this.stream, this.littleEndianBits ? this.byteBuffer : reverseBits(this.byteBuffer));
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
                NeutralEndian.Write1(this.stream, this.littleEndianBits ? this.byteBuffer : reverseBits(this.byteBuffer));
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

                NeutralEndian.Write1(this.stream, this.littleEndianBits ? this.byteBuffer : reverseBits(this.byteBuffer));
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
                NeutralEndian.Write1(this.stream, this.littleEndianBits ? bits : reverseBits(bits));
                this.byteBuffer = data;
                return true;
            }

            this.byteBuffer <<= size;
            this.byteBuffer |= data;
            this.waitingBits += size;
            return false;
        }

        public override byte reverseBits(byte val)
        {
            byte sz = 1 * 8;  // bit size; must be power of 2 
            byte mask = 0xFF;
            while ((sz >>= 1) > 0)
            {
                mask ^= (byte)(mask << sz);
                val = (byte)(((val >> sz) & mask) | ((val << sz) & ~mask));
            }
            return val;
        }
    }
}
