namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public sealed class UInt16BEInputBitStream : InputBitStream<ushort>
    {
        private Stream stream;
        private int remainingBits;
        private ushort byteBuffer;
        private bool littleEndianBits;
        private bool earlyDescriptor;

        public UInt16BEInputBitStream(Stream stream, bool littleEndianBits, bool earlyDescriptor)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.stream = stream;
            this.littleEndianBits = littleEndianBits;
            this.earlyDescriptor = earlyDescriptor;

            if (this.earlyDescriptor)
            {
                this.remainingBits = 16;
                this.byteBuffer = this.littleEndianBits ? BigEndian.Read2(stream) : reverseBits(BigEndian.Read2(stream));
            }
            else
            {
                this.remainingBits = 0;
            }
        }

        public override bool Get()
        {
            this.CheckBuffer();
            --this.remainingBits;
            ushort bit = (ushort)(this.byteBuffer & (1 << this.remainingBits));
            this.byteBuffer ^= bit; // clear the bit
            return bit != 0;
        }

        public override bool Pop()
        {
            if (!this.earlyDescriptor)
                this.CheckBuffer();

            --this.remainingBits;
            ushort bit;
            
            bit = (ushort)(this.byteBuffer & 1);
            this.byteBuffer >>= 1;

            if (this.earlyDescriptor)
                this.CheckBuffer();

            return bit != 0;
        }

        public override ushort Read(int count)
        {
            this.CheckBuffer();
            if (this.remainingBits < count)
            {
                int delta = count - this.remainingBits;
                ushort lowBits = (ushort)(this.byteBuffer << delta);
                this.byteBuffer = this.littleEndianBits ? BigEndian.Read2(stream) : reverseBits(BigEndian.Read2(stream));
                this.remainingBits = 16 - delta;
                ushort highBits = (ushort)(this.byteBuffer >> this.remainingBits);
                this.byteBuffer ^= (ushort)(highBits << this.remainingBits);
                return (ushort)(lowBits | highBits);
            }

            this.remainingBits -= count;
            ushort bits = (ushort)(this.byteBuffer >> this.remainingBits);
            this.byteBuffer ^= (ushort)(bits << this.remainingBits);
            return bits;
        }

        private void CheckBuffer()
        {
            if (this.remainingBits == 0)
            {
                this.byteBuffer = this.littleEndianBits ? BigEndian.Read2(stream) : reverseBits(BigEndian.Read2(stream));
                this.remainingBits = 16;
            }
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
