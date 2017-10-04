namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public sealed class UInt16BENonEarlyBEOutputBitStream : OutputBitStream<ushort>
    {
        private Stream stream;
        private int waitingBits;
        private ushort byteBuffer;

        public UInt16BENonEarlyBEOutputBitStream(Stream stream)
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

            if (this.waitingBits >= 16)
            {
                BigEndian.Write2(this.stream, this.byteBuffer);
                this.waitingBits = 0;
                this.byteBuffer = 0;
                flushed = true;
            }

            this.byteBuffer >>= 1;

            if (bit)
                this.byteBuffer |= 0x8000;

            ++this.waitingBits;

            return flushed;
        }

        public override bool Push(bool bit)
        {
            bool flushed = false;

            if (this.waitingBits >= 16)
            {
                BigEndian.Write2(this.stream, this.byteBuffer);
                this.waitingBits = 0;
                this.byteBuffer = 0;
                flushed = true;
            }

            if (bit)
                this.byteBuffer |= (ushort)(0x8000 >> this.waitingBits);

            ++this.waitingBits;

            return flushed;
        }

        public override bool Flush(bool unchanged)
        {
            if (this.waitingBits != 0)
            {
                if (!unchanged)
                {
                    this.byteBuffer >>= 16 - this.waitingBits;
                }

                BigEndian.Write2(this.stream, this.byteBuffer);
                this.waitingBits = 0;
                return true;
            }

            return false;
        }

        public override bool Write(ushort data, int size)
        {
            return false;
        }
    }
}
