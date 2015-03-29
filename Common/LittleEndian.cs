namespace SonicRetro.KensSharp
{
    using System.IO;

    public static class LittleEndian
    {
        public static ushort Read2(Stream stream)
        {
            byte[] bytes = new byte[2];
            if (stream.Read(bytes, 0, 2) != 2)
            {
                throw new EndOfStreamException();
            }

            return (ushort)((bytes[1] << 8) | bytes[0]);
        }

        public static void Write2(Stream stream, ushort value)
        {
            byte[] bytes = new byte[] { (byte)(value & 0xFF), (byte)(value >> 8) };
            stream.Write(bytes, 0, 2);
        }
    }
}
