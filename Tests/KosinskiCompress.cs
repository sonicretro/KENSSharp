namespace SonicRetro.KensSharp.Tests
{
    using System.Collections.Generic;
    using Xunit;

    public static class KosinskiCompress
    {
        public static readonly List<object[]> Data = new List<object[]>();

        static KosinskiCompress()
        {
            AddDataItem(
                new byte[] { },
                new byte[] { 0x02, 0x00, 0x00, 0xF0, 0x00 });
            AddDataItem(
                new byte[] { 0xDC },
                new byte[] { 0x05, 0x00, 0xDC, 0x00, 0xF0, 0x00 });
            AddDataItem(
                new byte[] { 0xDC, 0xDC },
                new byte[] { 0x0B, 0x00, 0xDC, 0xDC, 0x00, 0xF0, 0x00 });
            AddDataItem(
                new byte[] { 0xDC, 0xFE },
                new byte[] { 0x0B, 0x00, 0xDC, 0xFE, 0x00, 0xF0, 0x00 });
            AddDataItem(
                new byte[] { 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC },
                new byte[] { 0x15, 0x00, 0xDC, 0xFF, 0xF8, 0x0E, 0x00, 0xF0, 0x00 });
            AddDataItem(
                new byte[] { 0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE, 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF },
                new byte[] { 0xFF, 0xFF, 0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE, 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
                    0xCD, 0x02, 0x00, 0xEF, 0x00, 0xF0, 0x00 });
        }

        [Theory]
        [MemberData("Data")]
        public static void Compress(byte[] input, byte[] expectedOutput)
        {
            byte[] actualOutput = Kosinski.Compress(input);
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData("Data")]
        public static void CompressionThenDecompressionRoundtrips(byte[] input, byte[] ignored)
        {
            byte[] compressed = Kosinski.Compress(input);
            byte[] decompressed = Kosinski.Decompress(compressed);
            Assert.Equal(input, decompressed);
        }

        private static void AddDataItem(byte[] input, byte[] expectedOutput)
        {
            Data.Add(new[] { input, expectedOutput });
        }
    }
}
