namespace SonicRetro.KensSharp
{
    public abstract class OutputBitStream
    {
        public abstract bool Put(bool bit);

        public abstract bool Push(bool bit);

        public abstract bool Flush(bool unchanged);
    }

    public abstract class OutputBitStream<T> : OutputBitStream where T : struct
    {
        public abstract bool Write(T data, int size);
    }
}
