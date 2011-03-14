namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public sealed class UInt16BEInputBitStream : InputBitStream<ushort>
    {
        private Stream stream;
        private int remainingBits;
        private ushort byteBuffer;

        public UInt16BEInputBitStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.stream = stream;
            this.remainingBits = 16;
            this.byteBuffer = BigEndian.Read2(stream);
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
            --this.remainingBits;
            ushort bit = (ushort)(this.byteBuffer & 1);
            this.byteBuffer >>= 1;
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
                this.byteBuffer = BigEndian.Read2(stream);
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
                this.byteBuffer = BigEndian.Read2(stream);
                this.remainingBits = 16;
            }
        }
    }
}
