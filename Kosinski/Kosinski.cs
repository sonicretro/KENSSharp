namespace SonicRetro.KensSharp
{
    using System;
    using System.IO;

    public static partial class Kosinski
    {
        public static byte[] Decompress(string sourceFilePath, bool moduled)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Decompress(input, output, moduled);
                    return output.ToArray();
                }
            }
        }

        public static void Decompress(byte[] sourceData, string destinationFilePath, bool moduled)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Decompress(input, output, moduled);
                }
            }
        }

        public static void Decompress(string sourceFilePath, string destinationFilePath, bool moduled)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Decompress(input, output, moduled);
                }
            }
        }

        public static byte[] Decompress(byte[] sourceData, bool moduled)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Decompress(input, output, moduled);
                    return output.ToArray();
                }
            }
        }

        public static void Decompress(Stream input, Stream output, bool moduled)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            if (!Decode(input, output, 0, moduled))
            {
                throw new CompressionException("Decompression failed!");
            }
        }

        public static byte[] Compress(string sourceFilePath, bool moduled)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Compress(input, output, moduled);
                    return output.ToArray();
                }
            }
        }

        public static void Compress(byte[] sourceData, string destinationFilePath, bool moduled)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Compress(input, output, moduled);
                }
            }
        }

        public static void Compress(string sourceFilePath, string destinationFilePath, bool moduled)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            {
                using (FileStream output = File.Create(destinationFilePath))
                {
                    Compress(input, output, moduled);
                }
            }
        }

        public static byte[] Compress(byte[] sourceData, bool moduled)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    Compress(input, output, moduled);
                    return output.ToArray();
                }
            }
        }

        public static void Compress(Stream input, Stream output, bool moduled)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            if (!Encode(input, output, moduled))
            {
                throw new CompressionException("Compression failed!");
            }
        }
    }
}
