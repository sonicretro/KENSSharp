namespace SonicRetro.KensSharp
{
    public abstract class InputBitStream
    {
        public abstract bool Get();

        public abstract bool Pop();
    }

    public abstract class InputBitStream<T> : InputBitStream where T : struct
    {
        public abstract T Read(int count);
    }
}
