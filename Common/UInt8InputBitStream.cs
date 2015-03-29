namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public sealed class UInt8InputBitStream : InputBitStream<byte>
    {
        private Stream stream;
        private int remainingBits;
        private byte byteBuffer;

        public UInt8InputBitStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.stream = stream;
            this.remainingBits = 8;
            this.byteBuffer = NeutralEndian.Read1(stream);
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
            --this.remainingBits;
            byte bit = (byte)(this.byteBuffer & 1);
            this.byteBuffer >>= 1;
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
                this.byteBuffer = NeutralEndian.Read1(stream);
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
                this.byteBuffer = NeutralEndian.Read1(stream);
                this.remainingBits = 8;
            }
        }
    }
}
