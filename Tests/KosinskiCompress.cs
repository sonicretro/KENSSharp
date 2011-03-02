namespace SonicRetro.KensSharp.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public static class KosinskiCompress
    {
        private static readonly byte[] OneByteTestInput = new byte[] { 0xDC };
        private static readonly byte[] OneByteTestOutput = new byte[] { 0x0B, 0x00, 0xDC, 0x00, 0x00, 0xF0, 0x00 };

        private static readonly byte[] TwoIdenticalBytesTestInput = new byte[] { 0xDC, 0xDC };
        private static readonly byte[] TwoIdenticalBytesTestOutput = new byte[] { 0x0B, 0x00, 0xDC, 0xDC, 0x00, 0xF0, 0x00 };

        private static readonly byte[] TwoDifferentBytesTestInput = new byte[] { 0xDC, 0xFE };
        private static readonly byte[] TwoDifferentBytesTestOutput = new byte[] { 0x0B, 0x00, 0xDC, 0xFE, 0x00, 0xF0, 0x00 };

        private static readonly byte[] SixteenIdenticalBytesTestInput =
            new byte[] { 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC };
        private static readonly byte[] SixteenIdenticalBytesTestOutput =
            new byte[] { 0x15, 0x00, 0xDC, 0xFF, 0xF8, 0x0E, 0x00, 0xF0, 0x00 };

        private static readonly byte[] SixteenDifferentBytesTestInput =
            new byte[] { 0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE, 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };
        private static readonly byte[] SixteenDifferentBytesTestOutput =
            new byte[] { 0xFF, 0xFF, 0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE, 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
                0xCD, 0x02, 0x00, 0xEF, 0x00, 0xF0, 0x00 };

        [TestCase]
        public static void TestOneByte()
        {
            byte[] output = Kosinski.Compress(OneByteTestInput, false);
            CollectionAssert.AreEqual(OneByteTestOutput, output);
        }

        [TestCase]
        public static void TestTwoIdenticalBytes()
        {
            byte[] output = Kosinski.Compress(TwoIdenticalBytesTestInput, false);
            CollectionAssert.AreEqual(TwoIdenticalBytesTestOutput, output);
        }

        [TestCase]
        public static void TestTwoDifferentBytes()
        {
            byte[] output = Kosinski.Compress(TwoDifferentBytesTestInput, false);
            CollectionAssert.AreEqual(TwoDifferentBytesTestOutput, output);
        }

        [TestCase]
        public static void TestSixteenIdenticalBytes()
        {
            byte[] output = Kosinski.Compress(SixteenIdenticalBytesTestInput, false);
            CollectionAssert.AreEqual(SixteenIdenticalBytesTestOutput, output);
        }

        [TestCase]
        public static void TestSixteenDifferentBytes()
        {
            byte[] output = Kosinski.Compress(SixteenDifferentBytesTestInput, false);
            CollectionAssert.AreEqual(SixteenDifferentBytesTestOutput, output);
        }
    }
}
