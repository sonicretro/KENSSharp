namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public sealed class UInt16LEOutputBitStream : OutputBitStream<ushort>
    {
        private Stream stream;
        private int waitingBits;
        private ushort byteBuffer;

        public UInt16LEOutputBitStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.stream = stream;
        }

        public bool HasWaitingBits
        {
            get
            {
                return this.waitingBits != 0;
            }
        }

        public override bool Put(bool bit)
        {
            this.byteBuffer <<= 1;
            this.byteBuffer |= Convert.ToUInt16(bit);
            if (++this.waitingBits >= 16)
            {
                LittleEndian.Write2(this.stream, this.byteBuffer);
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
                LittleEndian.Write2(this.stream, this.byteBuffer);
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

                LittleEndian.Write2(this.stream, this.byteBuffer);
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
                LittleEndian.Write2(this.stream, (ushort)((this.byteBuffer << delta) | (data >> this.waitingBits)));
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
