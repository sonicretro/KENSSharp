namespace SonicRetro.KensSharp
{
    using System.IO;

    public static class BigEndian
    {
        public static ushort Read2(Stream stream)
        {
            byte[] bytes = new byte[2];
            if (stream.Read(bytes, 0, 2) != 2)
            {
                throw new EndOfStreamException();
            }

            return (ushort)((bytes[0] << 8) | bytes[1]);
        }

        public static void Write2(Stream stream, ushort value)
        {
            byte[] bytes = new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
            stream.Write(bytes, 0, 2);
        }
    }
}
