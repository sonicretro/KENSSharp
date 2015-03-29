namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public static partial class Saxman
    {
        public static byte[] Decompress(string sourceFilePath)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Decompress(input, output);
                    return output.ToArray();
                }
            }
        }

        public static byte[] Decompress(string sourceFilePath, long size)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Decompress(input, output, size);
                    return output.ToArray();
                }
            }
        }

        public static void Decompress(byte[] sourceData, string destinationFilePath)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Decompress(input, output);
                }
            }
        }

        public static void Decompress(byte[] sourceData, string destinationFilePath, long size)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Decompress(input, output, size);
                }
            }
        }

        public static void Decompress(string sourceFilePath, string destinationFilePath)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Decompress(input, output);
                }
            }
        }

        public static void Decompress(string sourceFilePath, string destinationFilePath, long size)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Decompress(input, output, size);
                }
            }
        }

        public static byte[] Decompress(byte[] sourceData)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Decompress(input, output);
                    return output.ToArray();
                }
            }
        }

        public static byte[] Decompress(byte[] sourceData, long size)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Decompress(input, output, size);
                    return output.ToArray();
                }
            }
        }

        public static void Decompress(Stream input, Stream output)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            Decode(input, output);
        }

        public static void Decompress(Stream input, Stream output, long size)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            Decode(input, output, size);
        }

        public static byte[] Compress(string sourceFilePath, bool withSize)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Compress(input, output, withSize);
                    return output.ToArray();
                }
            }
        }

        public static void Compress(byte[] sourceData, string destinationFilePath, bool withSize)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Compress(input, output, withSize);
                }
            }
        }

        public static void Compress(string sourceFilePath, string destinationFilePath, bool withSize)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Compress(input, output, withSize);
                }
            }
        }

        public static byte[] Compress(byte[] sourceData, bool withSize)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Compress(input, output, withSize);
                    return output.ToArray();
                }
            }
        }

        public static void Compress(Stream input, Stream output, bool withSize)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            Encode(input, output, withSize);
        }
    }
}
