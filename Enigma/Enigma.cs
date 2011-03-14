namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public static partial class Enigma
    {
        public static byte[] Decompress(string sourceFilePath, Endianness endianness)
        {
            ValidateEndianness(endianness);
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Decompress(input, output, endianness);
                    return output.ToArray();
                }
            }
        }

        public static void Decompress(byte[] sourceData, string destinationFilePath, Endianness endianness)
        {
            ValidateEndianness(endianness);
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Decompress(input, output, endianness);
                }
            }
        }

        public static void Decompress(string sourceFilePath, string destinationFilePath, Endianness endianness)
        {
            ValidateEndianness(endianness);
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Decompress(input, output, endianness);
                }
            }
        }

        public static byte[] Decompress(byte[] sourceData, Endianness endianness)
        {
            ValidateEndianness(endianness);
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Decompress(input, output, endianness);
                    return output.ToArray();
                }
            }
        }

        public static void Decompress(Stream input, Stream output, Endianness endianness)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            ValidateEndianness(endianness);
            Decode(input, output, endianness);
        }

        public static byte[] Compress(string sourceFilePath, Endianness endianness)
        {
            ValidateEndianness(endianness);
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Compress(input, output, endianness);
                    return output.ToArray();
                }
            }
        }

        public static void Compress(byte[] sourceData, string destinationFilePath, Endianness endianness)
        {
            ValidateEndianness(endianness);
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Compress(input, output, endianness);
                }
            }
        }

        public static void Compress(string sourceFilePath, string destinationFilePath, Endianness endianness)
        {
            ValidateEndianness(endianness);
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Compress(input, output, endianness);
                }
            }
        }

        public static byte[] Compress(byte[] sourceData, Endianness endianness)
        {
            ValidateEndianness(endianness);
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Compress(input, output, endianness);
                    return output.ToArray();
                }
            }
        }

        public static void Compress(Stream input, Stream output, Endianness endianness)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            ValidateEndianness(endianness);
            Encode(input, output, endianness);
        }

        private static void ValidateEndianness(Endianness endianness)
        {
            if (endianness != Endianness.BigEndian && endianness != Endianness.LittleEndian)
            {
                throw new ArgumentOutOfRangeException("endianness");
            }
        }
    }
}
