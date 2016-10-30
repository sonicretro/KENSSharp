namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public sealed class UInt8InputBitStream : InputBitStream<byte>
    {
        private Stream stream;
        private int remainingBits;
        private byte byteBuffer;
        private bool littleEndianBits;
        private bool earlyDescriptor;

        public UInt8InputBitStream(Stream stream, bool littleEndianBits, bool earlyDescriptor)
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
                this.remainingBits = 8;
                this.byteBuffer = this.littleEndianBits ? NeutralEndian.Read1(stream) : reverseBits(NeutralEndian.Read1(stream));
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
            byte bit = (byte)(this.byteBuffer & (1 << this.remainingBits));
            this.byteBuffer ^= bit; // clear the bit
            return bit != 0;
        }

        public override bool Pop()
        {
            if (!this.earlyDescriptor)
                this.CheckBuffer();

            --this.remainingBits;
            byte bit = (byte)(this.byteBuffer & 1);
            this.byteBuffer >>= 1;

            if (this.earlyDescriptor)
                this.CheckBuffer();

            return bit != 0;
        }

        public bool Unshift()
        {
            this.CheckBuffer();
            --this.remainingBits;
            byte bit = (byte)(this.byteBuffer & 1);
            this.byteBuffer >>= 1;
            return bit != 0;
        }

        public override byte Read(int count)
        {
            this.CheckBuffer();
            if (this.remainingBits < count)
            {
                int delta = count - this.remainingBits;
                byte lowBits = (byte)(this.byteBuffer << delta);
                this.byteBuffer = this.littleEndianBits ? NeutralEndian.Read1(stream) : reverseBits(NeutralEndian.Read1(stream));
                this.remainingBits = 8 - delta;
                ushort highBits = (byte)(this.byteBuffer >> this.remainingBits);
                this.byteBuffer ^= (byte)(highBits << this.remainingBits);
                return (byte)(lowBits | highBits);
            }

            this.remainingBits -= count;
            byte bits = (byte)(this.byteBuffer >> this.remainingBits);
            this.byteBuffer ^= (byte)(bits << this.remainingBits);
            return bits;
        }

        private void CheckBuffer()
        {
            if (this.remainingBits == 0)
            {
                this.byteBuffer = this.littleEndianBits ? NeutralEndian.Read1(stream) : reverseBits(NeutralEndian.Read1(stream));
                this.remainingBits = 8;
            }
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
