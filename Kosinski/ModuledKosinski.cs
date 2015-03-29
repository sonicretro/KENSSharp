namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public static class ModuledKosinski
    {
        public static byte[] Decompress(string sourceFilePath, Endianness headerEndianness)
        {
            ValidateEndianness(headerEndianness);
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Decompress(input, output, headerEndianness);
                    return output.ToArray();
                }
            }
        }

        public static void Decompress(byte[] sourceData, string destinationFilePath, Endianness headerEndianness)
        {
            ValidateEndianness(headerEndianness);
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Decompress(input, output, headerEndianness);
                }
            }
        }

        public static void Decompress(string sourceFilePath, string destinationFilePath, Endianness headerEndianness)
        {
            ValidateEndianness(headerEndianness);
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Decompress(input, output, headerEndianness);
                }
            }
        }

        public static byte[] Decompress(byte[] sourceData, Endianness headerEndianness)
        {
            ValidateEndianness(headerEndianness);
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Decompress(input, output, headerEndianness);
                    return output.ToArray();
                }
            }
        }

        public static void Decompress(Stream input, Stream output, Endianness headerEndianness)
        {
            ValidateEndianness(headerEndianness);
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            Kosinski.Decode(input, output, headerEndianness);
        }

        public static byte[] Compress(string sourceFilePath, Endianness headerEndianness)
        {
            ValidateEndianness(headerEndianness);
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Compress(input, output, headerEndianness);
                    return output.ToArray();
                }
            }
        }

        public static void Compress(byte[] sourceData, string destinationFilePath, Endianness headerEndianness)
        {
            ValidateEndianness(headerEndianness);
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Compress(input, output, headerEndianness);
                }
            }
        }

        public static void Compress(string sourceFilePath, string destinationFilePath, Endianness headerEndianness)
        {
            ValidateEndianness(headerEndianness);
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Compress(input, output, headerEndianness);
                }
            }
        }

        public static byte[] Compress(byte[] sourceData, Endianness headerEndianness)
        {
            ValidateEndianness(headerEndianness);
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Compress(input, output, headerEndianness);
                    return output.ToArray();
                }
            }
        }

        public static void Compress(Stream input, Stream output, Endianness headerEndianness)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            ValidateEndianness(headerEndianness);
            Kosinski.Encode(input, output, headerEndianness);
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
