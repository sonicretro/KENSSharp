namespace SonicRetro.KensSharp
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security;

    public static partial class Nemesis
    {
        private static byte[,] linearCoeffs2 = new byte[2, 2] {
            { 3, 0 },
            { 1, 1 } };

        private static byte[,] linearCoeffs3 = new byte[4, 3] {
            { 4, 0, 0 },
            { 2, 1, 0 },
            { 1, 0, 1 },
            { 0, 2, 0 }};

        private static byte[,] linearCoeffs4 = new byte[6, 4] {
            { 5, 0, 0, 0 },
            { 3, 1, 0, 0 },
            { 2, 0, 1, 0 },
            { 1, 2, 0, 0 },
            { 1, 0, 0, 1 },
            { 0, 1, 1, 0 } };

        private static byte[,] linearCoeffs5 = new byte[10, 5] {
            { 6, 0, 0, 0, 0 },
            { 4, 1, 0, 0, 0 },
            { 3, 0, 1, 0, 0 },
            { 2, 2, 0, 0, 0 },
            { 2, 0, 0, 1, 0 },
            { 1, 1, 1, 0, 0 },
            { 1, 0, 0, 0, 1 },
            { 0, 3, 0, 0, 0 },
            { 0, 1, 0, 1, 0 },
            { 0, 0, 2, 0, 0 } };

        private static byte[,] linearCoeffs6 = new byte[14, 6] {
            { 7, 0, 0, 0, 0, 0 },
            { 5, 1, 0, 0, 0, 0 },
            { 4, 0, 1, 0, 0, 0 },
            { 3, 2, 0, 0, 0, 0 },
            { 3, 0, 0, 1, 0, 0 },
            { 2, 1, 1, 0, 0, 0 },
            { 2, 0, 0, 0, 1, 0 },
            { 1, 3, 0, 0, 0, 0 },
            { 1, 1, 0, 1, 0, 0 },
            { 1, 0, 2, 0, 0, 0 },
            { 1, 0, 0, 0, 0, 1 },
            { 0, 2, 1, 0, 0, 0 },
            { 0, 1, 0, 0, 1, 0 },
            { 0, 0, 1, 1, 0, 0 } };

        private static byte[,] linearCoeffs7 = new byte[21, 7] {
            { 8, 0, 0, 0, 0, 0, 0 },
            { 6, 1, 0, 0, 0, 0, 0 },
            { 5, 0, 1, 0, 0, 0, 0 },
            { 4, 2, 0, 0, 0, 0, 0 },
            { 4, 0, 0, 1, 0, 0, 0 },
            { 3, 1, 1, 0, 0, 0, 0 },
            { 3, 0, 0, 0, 1, 0, 0 },
            { 2, 3, 0, 0, 0, 0, 0 },
            { 2, 1, 0, 1, 0, 0, 0 },
            { 2, 0, 2, 0, 0, 0, 0 },
            { 2, 0, 0, 0, 0, 1, 0 },
            { 1, 2, 1, 0, 0, 0, 0 },
            { 1, 1, 0, 0, 1, 0, 0 },
            { 1, 0, 1, 1, 0, 0, 0 },
            { 1, 0, 0, 0, 0, 0, 1 },
            { 0, 4, 0, 0, 0, 0, 0 },
            { 0, 2, 0, 1, 0, 0, 0 },
            { 0, 1, 2, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0, 1, 0 },
            { 0, 0, 1, 0, 1, 0, 0 },
            { 0, 0, 0, 2, 0, 0, 0 } };

        private static byte[][,] linearCoeffs = new byte[][,] { linearCoeffs2, linearCoeffs3, linearCoeffs4, linearCoeffs5, linearCoeffs6, linearCoeffs7 };

        private static void Encode(Stream input, Stream output)
        {
            using (PaddedStream paddedInput = new PaddedStream(input, 32, PaddedStreamMode.Read))
            {
                using (XorStream xorPaddedInput = new XorStream(paddedInput))
                {
                    using (MemoryStream normalOutput = new MemoryStream())
                    {
                        using (MemoryStream xorOutput = new MemoryStream())
                        {
                            long initialPosition = input.Position;
                            long inputLength = input.Length - initialPosition;

                            // Compress using normal encoding
                            EncodeInternal(paddedInput, normalOutput, false, inputLength);

                            // Reset the input stream and compress using XOR encoding
                            input.Position = initialPosition;
                            EncodeInternal(xorPaddedInput, xorOutput, true, inputLength);

                            long normalOutputLength = normalOutput.Length;
                            long xorOutputLength = xorOutput.Length;

                            using (PaddedStream paddedOutput = new PaddedStream(output, 2, PaddedStreamMode.Write))
                            {
                                byte[] outputBytes =
                                    (normalOutputLength <= xorOutputLength ? normalOutput : xorOutput).ToArray();
                                paddedOutput.Write(outputBytes, 0, outputBytes.Length);
                            }
                        }
                    }
                }
            }
        }

        private static void EncodeInternal(Stream input, Stream output, bool xor, long inputLength)
        {
            var rleSource = new List<NibbleRun>();
            var counts = new SortedList<NibbleRun, long>();

            using (IEnumerator<byte> unpacked = Unpacked(input))
            {
                // Build RLE nibble runs, RLE-encoding the nibble runs as we go along.
                // Maximum run length is 8, meaning 7 repetitions.
                if (unpacked.MoveNext())
                {
                    NibbleRun current = new NibbleRun(unpacked.Current, 0);
                    while (unpacked.MoveNext())
                    {
                        NibbleRun next = new NibbleRun(unpacked.Current, 0);
                        if (next.Nibble != current.Nibble || current.Count >= 7)
                        {
                            rleSource.Add(current);
                            long count;
                            counts.TryGetValue(current, out count);
                            counts[current] = count + 1;
                            current = next;
                        }
                        else
                        {
                            ++current.Count;
                        }
                    }
                }
            }

            // We will use the Package-merge algorithm to build the optimal length-limited
            // Huffman code for the current file. To do this, we must map the current
            // problem onto the Coin Collector's problem.
            // Build the basic coin collection.
            var qt = new List<EncodingCodeTreeNode>();
            foreach (var kvp in counts)
            {
                // No point in including anything with weight less than 2, as they
                // would actually increase compressed file size if used.
                if (kvp.Value > 1)
                {
                    qt.Add(new EncodingCodeTreeNode(kvp.Key, kvp.Value));
                }
            }

            qt.Sort();

            // The base coin collection for the length-limited Huffman coding has
            // one coin list per character in length of the limmitation. Each coin list
            // has a constant "face value", and each coin in a list has its own
            // "numismatic value". The "face value" is unimportant in the way the code
            // is structured below; the "numismatic value" of each coin is the number
            // of times the underlying nibble run appears in the source file.

            // This will hold the Huffman code map.
            // NOTE: while the codes that will be written in the header will not be
            // longer than 8 bits, it is possible that a supplementary code map will
            // add "fake" codes that are longer than 8 bits.
            var codeMap = new SortedList<NibbleRun, KeyValuePair<long, byte>>();

            // Size estimate. This is used to build the optimal compressed file.
            long sizeEstimate = long.MaxValue;

            // We will solve the Coin Collector's problem several times, each time
            // ignoring more of the least frequent nibble runs. This allows us to find
            // *the* lowest file size.
            while (qt.Count > 1)
            {
                // Make a copy of the basic coin collection.
                var q0 = new List<EncodingCodeTreeNode>(qt);

                // Ignore the lowest weighted item. Will only affect the next iteration
                // of the loop. If it can be proven that there is a single global
                // minimum (and no local minima for file size), then this could be
                // simplified to a binary search.
                qt.RemoveAt(qt.Count - 1);

                // We now solve the Coin collector's problem using the Package-merge
                // algorithm. The solution goes here.
                var solution = new List<EncodingCodeTreeNode>();

                // This holds the packages from the last iteration.
                var q = new List<EncodingCodeTreeNode>(q0);

                int target = (q0.Count - 1) << 8, idx = 0;
                while (target != 0)
                {
                    // Gets lowest bit set in its proper place:
                    int val = (target & -target), r = 1 << idx;

                    // Is the current denomination equal to the least denomination?
                    if (r == val)
                    {
                        // If yes, take the least valuable node and put it into the solution.
                        solution.Add(q[q.Count - 1]);
                        q.RemoveAt(q.Count - 1);
                        target -= r;
                    }

                    // The coin collection has coins of values 1 to 8; copy from the
                    // original in those cases for the next step.
                    var q1 = new List<EncodingCodeTreeNode>();
                    if (idx < 7)
                    {
                        q1.AddRange(q0);
                    }

                    // Split the current list into pairs and insert the packages into
                    // the next list.
                    while (q.Count > 1)
                    {
                        EncodingCodeTreeNode child1 = q[q.Count - 1];
                        q.RemoveAt(q.Count - 1);
                        EncodingCodeTreeNode child0 = q[q.Count - 1];
                        q.RemoveAt(q.Count - 1);
                        q1.Add(new EncodingCodeTreeNode(child0, child1));
                    }

                    idx++;
                    q.Clear();
                    q.AddRange(q1);
                    q.Sort();
                }

                // The Coin Collector's problem has been solved. Now it is time to
                // map the solution back into the length-limited Huffman coding problem.

                // To do that, we iterate through the solution and count how many times
                // each nibble run has been used (remember that the coin collection had
                // had multiple coins associated with each nibble run) -- this number
                // is the optimal bit length for the nibble run.
                var baseSizeMap = new SortedList<NibbleRun, long>();
                foreach (var item in solution)
                {
                    item.Traverse(baseSizeMap);
                }

                // With the length-limited Huffman coding problem solved, it is now time
                // to build the code table. As input, we have a map associating a nibble
                // run to its optimal encoded bit length. We will build the codes using
                // the canonical Huffman code.

                // To do that, we must invert the size map so we can sort it by code size.
                var sizeOnlyMap = new MultiSet<long>();

                // This map contains lots more information, and is used to associate
                // the nibble run with its optimal code. It is sorted by code size,
                // then by frequency of the nibble run, then by the nibble run.
                var sizeMap = new MultiSet<SizeMapItem>();

                foreach (var item in baseSizeMap)
                {
                    long size = item.Value;
                    sizeOnlyMap.Add(size);
                    sizeMap.Add(new SizeMapItem(size, counts[item.Key], item.Key));
                }

                // We now build the canonical Huffman code table.
                // "baseCode" is the code for the first nibble run with a given bit length.
                // "carry" is how many nibble runs were demoted to a higher bit length
                // at an earlier step.
                // "cnt" is how many nibble runs have a given bit length.
                long baseCode = 0;
                long carry = 0, cnt;

                // This list contains the codes sorted by size.
                var codes = new List<KeyValuePair<long, byte>>();
                for (byte j = 1; j <= 8; j++)
                {
                    // How many nibble runs have the desired bit length.
                    cnt = sizeOnlyMap.Count(j) + carry;
                    carry = 0;

                    for (int k = 0; k < cnt; k++)
                    {
                        // Sequential binary numbers for codes.
                        long code = baseCode + k;
                        long mask = (1L << j) - 1;

                        // We do not want any codes composed solely of 1's or which
                        // start with 111111, as that sequence is reserved.
                        if ((j <= 6 && code == mask) ||
                            (j > 6 && code == (mask & ~((1L << (j - 6)) - 1))))
                        {
                            // We must demote this many nibble runs to a longer code.
                            carry = cnt - k;
                            cnt = k;
                            break;
                        }

                        codes.Add(new KeyValuePair<long, byte>(code, j));
                    }

                    // This is the beginning bit pattern for the next bit length.
                    baseCode = (baseCode + cnt) << 1;
                }

                // With the canonical table build, the codemap can finally be built.
                var tempCodemap = new SortedList<NibbleRun, KeyValuePair<long, byte>>();
                using (IEnumerator<SizeMapItem> enumerator = sizeMap.GetEnumerator())
                {
                    int pos = 0;
                    while (enumerator.MoveNext() && pos < codes.Count)
                    {
                        tempCodemap[enumerator.Current.NibbleRun] = codes[pos];
                        ++pos;
                    }
                }

                // We now compute the final file size for this code table.
                // 2 bytes at the start of the file, plus 1 byte at the end of the
                // code table.
                long tempsize_est = 3 * 8;
                byte last = 0xff;

                // Start with any nibble runs with their own code.
                foreach (var item in tempCodemap)
                {
                    // Each new nibble needs an extra byte.
                    if (item.Key.Nibble != last)
                    {
                        tempsize_est += 8;
                        last = item.Key.Nibble;
                    }

                    // 2 bytes per nibble run in the table.
                    tempsize_est += 2 * 8;

                    // How many bits this nibble run uses in the file.
                    tempsize_est += counts[item.Key] * item.Value.Value;
                }

                // Supplementary code map for the nibble runs that can be broken up into
                // shorter nibble runs with a smaller bit length than inlining.
                var supCodemap = new Dictionary<NibbleRun, KeyValuePair<long, byte>>();

                // Now we will compute the size requirements for inline nibble runs.
                foreach (var item in counts)
                {
                    if (!tempCodemap.ContainsKey(item.Key))
                    {
                        // Nibble run does not have its own code. We need to find out if
                        // we can break it up into smaller nibble runs with total code
                        // size less than 13 bits or if we need to inline it (13 bits).
                        if (item.Key.Count == 0)
                        {
                            // If this is a nibble run with zero repeats, we can't break
                            // it up into smaller runs, so we inline it.
                            tempsize_est += (6 + 7) * item.Value;
                        }
                        else if (item.Key.Count == 1)
                        {
                            // We stand a chance of breaking the nibble run.
                            
                            // This case is rather trivial, so we hard-code it.
                            // We can break this up only as 2 consecutive runs of a nibble
                            // run with count == 0.
                            KeyValuePair<long, byte> value;
                            if (!tempCodemap.TryGetValue(new NibbleRun(item.Key.Nibble, 0), out value) || value.Value > 6)
                            {
                                // The smaller nibble run either does not have its own code
                                // or it results in a longer bit code when doubled up than
                                // would result from inlining the run. In either case, we
                                // inline the nibble run.
                                tempsize_est += (6 + 7) * item.Value;
                            }
                            else
                            {
                                // The smaller nibble run has a small enough code that it is
                                // more efficient to use it twice than to inline our nibble
                                // run. So we do exactly that, by adding a (temporary) entry
                                // in the supplementary codemap, which will later be merged
                                // into the main codemap.
                                long code = value.Key;
                                byte len = value.Value;
                                code = (code << len) | code;
                                len <<= 1;
                                tempsize_est += len * item.Value;
                                supCodemap[item.Key] = new KeyValuePair<long, byte>(code, (byte)(0x80 | len));
                            }
                        }
                        else
                        {
                            // We stand a chance of breaking the nibble run.
                            byte n = item.Key.Count;
                            
                            // This is a linear optimization problem subjected to 2
                            // constraints. If the number of repeats of the current nibble
                            // run is N, then we have N dimensions.
                            // Reference to table of linear coefficients. This table has
                            // N columns for each line.
                            byte[,] myLinearCoeffs = linearCoeffs[n - 2];
                            int rows = myLinearCoeffs.GetLength(0);

                            byte nibble = item.Key.Nibble;

                            // List containing the code length of each nibble run, or 13
                            // if the nibble run is not in the codemap.
                            var runlen = new List<long>();

                            // Initialize the list.
                            for (byte i = 0; i < n; i++)
                            {
                                // Is this run in the codemap?
                                KeyValuePair<long, byte> value;
                                if (tempCodemap.TryGetValue(new NibbleRun(nibble, i), out value))
                                {
                                    // It is.
                                    // Put code length in the vector.
                                    runlen.Add(value.Value);
                                }
                                else
                                {
                                    // It is not.
                                    // Put inline length in the vector.
                                    runlen.Add(6 + 7);
                                }
                            }

                            // Now go through the linear coefficient table and tally up
                            // the total code size, looking for the best case.
                            // The best size is initialized to be the inlined case.
                            long bestSize = 6 + 7;
                            int bestLine = -1;
                            for (int i = 0; i < rows; i++)
                            {
                                // Tally up the code length for this coefficient line.
                                long len = 0;
                                for (byte j = 0; j < n; j++)
                                {
                                    byte c = myLinearCoeffs[i, j];
                                    if (c == 0)
                                    {
                                        continue;
                                    }
                                    
                                    len += c * runlen[j];
                                }

                                // Is the length better than the best yet?
                                if (len < bestSize)
                                {
                                    // If yes, store it as the best.
                                    bestSize = len;
                                    bestLine = i;
                                }
                            }

                            // Have we found a better code than inlining?
                            if (bestLine >= 0)
                            {
                                // We have; use it. To do so, we have to build the code
                                // and add it to the supplementary code table.
                                long code = 0, len = 0;
                                for (byte i = 0; i < n; i++)
                                {
                                    byte c = myLinearCoeffs[bestLine, i];
                                    if (c == 0)
                                    {
                                        continue;
                                    }

                                    // Is this run in the codemap?
                                    KeyValuePair<long, byte> value;
                                    if (tempCodemap.TryGetValue(new NibbleRun(nibble, i), out value))
                                    {
                                        // It is; it MUST be, as the other case is impossible
                                        // by construction.
                                        for (int j = 0; j < c; j++)
                                        {
                                            len += value.Value;
                                            code <<= value.Value;
                                            code |= value.Key;
                                        }
                                    }
                                }

                                if (len != bestSize)
                                {
                                    // ERROR! DANGER! THIS IS IMPOSSIBLE!
                                    // But just in case...
                                    tempsize_est += (6 + 7) * item.Value;
                                }
                                else
                                {
                                    // By construction, best_size is at most 12.
                                    byte c = (byte)bestSize;

                                    // Add it to supplementary code map.
                                    supCodemap[item.Key] = new KeyValuePair<long, byte>(code, (byte)(0x80 | c));
                                    tempsize_est += bestSize * item.Value;
                                }
                            }
                            else
                            {
                                // No, we will have to inline it.
                                tempsize_est += (6 + 7) * item.Value;
                            }
                        }
                    }
                }

                // Merge the supplementary code map into the temporary code map.
                foreach (var item in supCodemap)
                {
                    tempCodemap[item.Key] = item.Value;
                }

                // Round up to a full byte.
                tempsize_est = (tempsize_est + 7) & ~7;

                // Is this iteration better than the best?
                if (tempsize_est < sizeEstimate)
                {
                    // If yes, save the codemap and file size.
                    codeMap = tempCodemap;
                    sizeEstimate = tempsize_est;
                }
            }

            // We now have a prefix-free code map associating the RLE-encoded nibble
            // runs with their code. Now we write the file.
            // Write header.
            BigEndian.Write2(output, (ushort)((Convert.ToInt32(xor) << 15) | ((int)inputLength >> 5)));
            byte lastNibble = 0xff;
            foreach (var item in codeMap)
            {
                byte length = item.Value.Value;

                // length with bit 7 set is a special device for further reducing file size, and
                // should NOT be on the table.
                if ((length & 0x80) != 0)
                {
                    continue;
                }

                NibbleRun nibbleRun = item.Key;
                if (nibbleRun.Nibble != lastNibble)
                {
                    // 0x80 marks byte as setting a new nibble.
                    NeutralEndian.Write1(output, (byte)(0x80 | nibbleRun.Nibble));
                    lastNibble = nibbleRun.Nibble;
                }

                long code = item.Value.Key;
                NeutralEndian.Write1(output, (byte)((nibbleRun.Count << 4) | length));
                NeutralEndian.Write1(output, (byte)code);
            }

            // Mark end of header.
            NeutralEndian.Write1(output, 0xff);

            // Write the encoded bitstream.
            UInt8OutputBitStream bitStream = new UInt8OutputBitStream(output);

            // The RLE-encoded source makes for a far faster encode as we simply
            // use the nibble runs as an index into the map, meaning a quick binary
            // search gives us the code to use (if in the map) or tells us that we
            // need to use inline RLE.
            foreach (var nibbleRun in rleSource)
            {
                KeyValuePair<long, byte> value;
                if (codeMap.TryGetValue(nibbleRun, out value))
                {
                    long code = value.Key;
                    byte len = value.Value;

                    // len with bit 7 set is a device to bypass the code table at the
                    // start of the file. We need to clear the bit here before writing
                    // the code to the file.
                    len &= 0x7f;

                    // We can have codes in the 9-12 range due to the break up of large
                    // inlined runs into smaller non-inlined runs. Deal with those high
                    // bits first, if needed.
                    if (len > 8)
                    {
                        bitStream.Write((byte)((code >> 8) & 0xff), len - 8);
                        len = 8;
                    }

                    bitStream.Write((byte)(code & 0xff), len);
                }
                else
                {
                    bitStream.Write(0x3f, 6);
                    bitStream.Write(nibbleRun.Count, 3);
                    bitStream.Write(nibbleRun.Nibble, 4);
                }
            }

            // Fill remainder of last byte with zeroes and write if needed.
            bitStream.Flush(false);
        }

        private static void Decode(Stream input, Stream output)
        {
            DecodingCodeTreeNode codeTree = new DecodingCodeTreeNode();
            ushort numberOfTiles = BigEndian.Read2(input);
            bool xorOutput = (numberOfTiles & 0x8000) != 0;
            numberOfTiles &= 0x7fff;
            DecodeHeader(input, output, codeTree);
            DecodeInternal(input, output, codeTree, numberOfTiles, xorOutput);
        }

        private static void DecodeHeader(Stream input, Stream output, DecodingCodeTreeNode codeTree)
        {
            byte outputValue = 0;
            byte inputValue;

            // Loop until a byte with value 0xFF is encountered
            while ((inputValue = NeutralEndian.Read1(input)) != 0xFF)
            {
                if ((inputValue & 0x80) != 0)
                {
                    outputValue = (byte)(inputValue & 0xF);
                    inputValue = NeutralEndian.Read1(input);
                }

                codeTree.SetCode(
                    NeutralEndian.Read1(input),
                    inputValue & 0xF,
                    new NibbleRun(outputValue, (byte)(((inputValue & 0x70) >> 4) + 1)));
            }

            // Store a special nibble run for inline RLE sequences (code = 0b111111, length = 6)
            // Length = 0xFF in the nibble run is just a marker value that will be handled specially in DecodeInternal
            codeTree.SetCode(0x3F, 6, new NibbleRun(0, 0xFF));
        }

        private static void DecodeInternal(Stream input, Stream output, DecodingCodeTreeNode codeTree, ushort numberOfTiles, bool xorOutput)
        {
            UInt8InputBitStream inputBits = new UInt8InputBitStream(input);
            UInt8OutputBitStream outputBits;
            XorStream xorStream = null;
            try
            {
                if (xorOutput)
                {
                    xorStream = new XorStream(output);
                    outputBits = new UInt8OutputBitStream(xorStream);
                }
                else
                {
                    outputBits = new UInt8OutputBitStream(output);
                }

                // The output is: number of tiles * 0x20 (1 << 5) bytes per tile * 8 (1 << 3) bits per byte
                int outputSize = numberOfTiles << 8; // in bits
                int bitsWritten = 0;

                DecodingCodeTreeNode currentNode = codeTree;
                while (bitsWritten < outputSize)
                {
                    NibbleRun nibbleRun = currentNode.NibbleRun;
                    if (nibbleRun.Count == 0xFF)
                    {
                        // Bit pattern 0b111111; inline RLE.
                        // First 3 bits are repetition count, followed by the inlined nibble.
                        byte count = (byte)(inputBits.Read(3) + 1);
                        byte nibble = inputBits.Read(4);
                        DecodeNibbleRun(inputBits, outputBits, count, nibble, ref bitsWritten);
                        currentNode = codeTree;
                    }
                    else if (nibbleRun.Count != 0)
                    {
                        // Output the encoded nibble run
                        DecodeNibbleRun(inputBits, outputBits, nibbleRun.Count, nibbleRun.Nibble, ref bitsWritten);
                        currentNode = codeTree;
                    }
                    else
                    {
                        // Read the next bit and go down one level in the tree
                        currentNode = currentNode[inputBits.Get()];
                        if (currentNode == null)
                        {
                            throw new CompressionException(Properties.Resources.InvalidCode);
                        }
                    }
                }

                outputBits.Flush(false);
            }
            finally
            {
                if (xorStream != null)
                {
                    xorStream.Dispose();
                }
            }
        }

        private static void DecodeNibbleRun(UInt8InputBitStream inputBits, UInt8OutputBitStream outputBits, byte count, byte nibble, ref int bitsWritten)
        {
            bitsWritten += count * 4;

            // Write single nibble, if needed
            if ((count & 1) != 0)
            {
                outputBits.Write(nibble, 4);
            }

            // Write pairs of nibbles
            count >>= 1;
            nibble |= (byte)(nibble << 4);
            while (count-- != 0)
            {
                outputBits.Write(nibble, 8);
            }
        }

        private static IEnumerator<byte> Unpacked(Stream stream)
        {
            int b;
            while ((b = stream.ReadByte()) != -1)
            {
                yield return (byte)((b & 0xf0) >> 4);
                yield return (byte)(b & 0xf);
            }

            yield return 0xff;
        }

        private struct NibbleRun : IComparable<NibbleRun>
        {
            public byte Nibble;
            public byte Count;

            public NibbleRun(byte nibble, byte count)
                : this()
            {
                this.Nibble = nibble;
                this.Count = count;
            }

            public override string ToString()
            {
                return this.Count.ToString() + " × " + this.Nibble.ToString("X");
            }

            public int CompareTo(NibbleRun other)
            {
                int comp = this.Nibble.CompareTo(other.Nibble);
                if (comp == 0)
                {
                    comp = this.Count.CompareTo(other.Count);
                }

                return comp;
            }
        }

        private struct SizeMapItem : IComparable<SizeMapItem>
        {
            public SizeMapItem(long codeSize, long frequency, NibbleRun nibbleRun)
                : this()
            {
                this.CodeSize = codeSize;
                this.Frequency = frequency;
                this.NibbleRun = nibbleRun;
            }

            public long CodeSize { get; private set; }

            public long Frequency { get; private set; }

            public NibbleRun NibbleRun { get; private set; }

            public int CompareTo(SizeMapItem other)
            {
                int comp = this.CodeSize.CompareTo(other.CodeSize);
                if (comp == 0)
                {
                    comp = this.Frequency.CompareTo(other.Frequency);
                    if (comp == 0)
                    {
                        comp = this.NibbleRun.CompareTo(other.NibbleRun);
                    }
                }

                return comp;
            }
        }

        private sealed class EncodingCodeTreeNode : IComparable<EncodingCodeTreeNode>
        {
            private EncodingCodeTreeNode clear;
            private EncodingCodeTreeNode set;
            private long weight;
            private NibbleRun nibbleRun;

            public EncodingCodeTreeNode(NibbleRun nibbleRun, long weight)
            {
                this.nibbleRun = nibbleRun;
                this.weight = weight;
            }

            public EncodingCodeTreeNode(EncodingCodeTreeNode clear, EncodingCodeTreeNode set)
            {
                this.clear = clear;
                this.set = set;
                this.weight = clear.weight + set.weight;
            }

            public int CompareTo(EncodingCodeTreeNode other)
            {
                return other.weight.CompareTo(this.weight);
            }

            public void Traverse(SortedList<NibbleRun, long> sizeMap)
            {
                if (this.clear == null && this.set == null)
                {
                    long count;
                    sizeMap.TryGetValue(this.nibbleRun, out count);
                    sizeMap[this.nibbleRun] = count + 1;
                }
                else
                {
                    if (this.clear != null)
                    {
                        this.clear.Traverse(sizeMap);
                    }

                    if (this.set != null)
                    {
                        this.set.Traverse(sizeMap);
                    }
                }
            }
        }

        private sealed class DecodingCodeTreeNode
        {
            private DecodingCodeTreeNode clear;
            private DecodingCodeTreeNode set;
            private NibbleRun nibbleRun;

            public void SetCode(byte code, int length, NibbleRun nibbleRun)
            {
                if (length == 0)
                {
                    if (this.clear != null || this.set != null)
                    {
                        throw new CompressionException(Properties.Resources.CodeAlreadyUsedAsPrefix);
                    }

                    this.nibbleRun = nibbleRun;
                }
                else
                {
                    if (this.nibbleRun.Count != 0)
                    {
                        throw new CompressionException(Properties.Resources.PrefixAlreadyUsedAsCode);
                    }

                    --length;
                    if ((code & (1 << length)) == 0)
                    {
                        if (this.clear == null)
                        {
                            this.clear = new DecodingCodeTreeNode();
                        }

                        this.clear.SetCode(code, length, nibbleRun);
                    }
                    else
                    {
                        if (this.set == null)
                        {
                            this.set = new DecodingCodeTreeNode();
                        }

                        this.set.SetCode((byte)(code & ((1 << length) - 1)), length, nibbleRun);
                    }
                }
            }

            public DecodingCodeTreeNode this[bool side]
            {
                get
                {
                    return side ? this.set : this.clear;
                }
            }

            public NibbleRun NibbleRun
            {
                get
                {
                    return this.nibbleRun;
                }
            }
        }

        private sealed class MultiSet<T> : IEnumerable<T>
        {
            private SortedList<T, long> set;

            public MultiSet()
            {
                this.set = new SortedList<T, long>();
            }

            public void Add(T value)
            {
                long count;
                this.set.TryGetValue(value, out count);
                this.set[value] = count + 1;
            }

            public long Count(T value)
            {
                long count;
                this.set.TryGetValue(value, out count);
                return count;
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var item in this.set)
                {
                    for (long i = 0; i < item.Value; i++)
                    {
                        yield return item.Key;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        private sealed class XorStream : Stream
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct ByteBuffer
            {
                internal unsafe fixed byte bytes[4];
            }

            private static string TypeName = typeof(XorStream).FullName;

            private Stream stream;
            private int subPosition; // 0-3
            private ByteBuffer bytes;

            public XorStream(Stream stream)
            {
                if (stream == null)
                {
                    throw new ArgumentNullException("stream");
                }

                this.stream = stream;
                this.bytes = new ByteBuffer();
            }

            public override bool CanRead
            {
                get { throw new System.NotImplementedException(); }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override void Flush()
            {
                if (this.stream == null)
                {
                    throw new ObjectDisposedException(TypeName);
                }

                this.stream.Flush();
            }

            public override long Length
            {
                get
                {
                    if (this.stream == null)
                    {
                        throw new ObjectDisposedException(TypeName);
                    }

                    return this.stream.Length;
                }
            }

            public override long Position
            {
                get
                {
                    if (this.stream == null)
                    {
                        throw new ObjectDisposedException(TypeName);
                    }

                    return this.stream.Position;
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            [SecuritySafeCritical]
            public unsafe override int Read(byte[] buffer, int offset, int count)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException("buffer");
                }

                if (offset < 0)
                {
                    throw new ArgumentException(Properties.Resources.NegativeOffset, "offset");
                }

                if (count < 0)
                {
                    throw new ArgumentException(Properties.Resources.NegativeCount, "count");
                }

                if (offset > buffer.Length)
                {
                    throw new ArgumentException(Properties.Resources.OffsetIsGreaterThanBufferSize, "offset");
                }

                if (offset + count > buffer.Length)
                {
                    throw new ArgumentException(Properties.Resources.OffsetPlusCountIsGreaterThanBufferSize);
                }

                if (this.stream == null)
                {
                    throw new ObjectDisposedException(TypeName);
                }

                int readBytes = this.stream.Read(buffer, offset, count);
                fixed (byte* bytes = this.bytes.bytes)
                {
                    for (int i = 0; i < readBytes; i++)
                    {
                        byte b = buffer[offset + i];
                        buffer[offset + i] ^= bytes[this.subPosition];
                        bytes[this.subPosition] = b;
                        ++this.subPosition;
                        this.subPosition &= 3;
                    }
                }

                return readBytes;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            [SecuritySafeCritical]
            public unsafe override void Write(byte[] buffer, int offset, int count)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException("buffer");
                }

                if (offset < 0)
                {
                    throw new ArgumentException(Properties.Resources.NegativeOffset, "offset");
                }

                if (count < 0)
                {
                    throw new ArgumentException(Properties.Resources.NegativeCount, "count");
                }

                if (offset > buffer.Length)
                {
                    throw new ArgumentException(Properties.Resources.OffsetIsGreaterThanBufferSize, "offset");
                }

                if (offset + count > buffer.Length)
                {
                    throw new ArgumentException(Properties.Resources.OffsetPlusCountIsGreaterThanBufferSize);
                }

                if (this.stream == null)
                {
                    throw new ObjectDisposedException(TypeName);
                }

                if (count == 0)
                {
                    return;
                }

                byte[] xorBuffer = new byte[count];
                fixed (byte* bytes = this.bytes.bytes)
                {
                    for (int i = 0; i < count; i++)
                    {
                        bytes[this.subPosition] ^= buffer[i];
                        xorBuffer[i] = bytes[this.subPosition];
                        ++this.subPosition;
                        this.subPosition &= 3;
                    }
                }

                this.stream.Write(xorBuffer, 0, count);
            }
        }
    }
}
