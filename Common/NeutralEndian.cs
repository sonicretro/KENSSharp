namespace SonicRetro.KensSharp
{
    using System.IO;

    public static class NeutralEndian
    {
        public static byte Read1(Stream stream)
        {
            int value = stream.ReadByte();
            if (value == -1)
            {
                throw new EndOfStreamException();
            }

            return (byte)value;
        }

        public static void Write1(Stream stream, byte value)
        {
            stream.WriteByte(value);
        }
    }
}
