namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public sealed class UInt16BEOutputBitStream : OutputBitStream<ushort>
    {
        private Stream stream;
        private int waitingBits;
        private ushort byteBuffer;
        private bool littleEndianBits;

        public UInt16BEOutputBitStream(Stream stream, bool littleEndianBits)
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
            this.byteBuffer |= Convert.ToUInt16(bit);
            if (++this.waitingBits >= 16)
            {
                BigEndian.Write2(this.stream, this.littleEndianBits ? this.byteBuffer : reverseBits(this.byteBuffer));
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
                BigEndian.Write2(this.stream, this.littleEndianBits ? this.byteBuffer : reverseBits(this.byteBuffer));
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

                BigEndian.Write2(this.stream, this.littleEndianBits ? this.byteBuffer : reverseBits(this.byteBuffer));
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
                BigEndian.Write2(this.stream, this.littleEndianBits ? bits : reverseBits(bits));
                this.byteBuffer = data;
                return true;
            }

            this.byteBuffer <<= size;
            this.byteBuffer |= data;
            this.waitingBits += size;
            return false;
        }

        public override ushort reverseBits(ushort val)
        {
            ushort sz = 2 * 8;  // bit size; must be power of 2 
            ushort mask = 0xFFFF;
            while ((sz >>= 1) > 0)
            {
                mask ^= (ushort)(mask << sz);
                val = (ushort)(((val >> sz) & mask) | ((val << sz) & ~mask));
            }
            return val;
        }
    }
}
