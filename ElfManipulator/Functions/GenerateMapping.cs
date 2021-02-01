using System;
using System.Collections.Generic;
using System.Text;
using ElfManipulator.Data;
using Yarhl.Media.Text;

namespace ElfManipulator.Functions
{
    internal class GenerateMapping
    {
        private byte[] elf;
        private Po po;
        private List<ElfData> data;
        private Encoding encoding;
        private int memDiff;
        private bool containsFixedLengthEntries;

        public GenerateMapping(byte[] elfOri, Po poPassed, Encoding encodingPassed, int memDiffPassed, bool containsFixedLength)
        {
            elf = elfOri;
            po = poPassed;
            encoding = encodingPassed;
            memDiff = memDiffPassed;
            containsFixedLengthEntries = containsFixedLength;
            data = new List<ElfData>();
        }

        public List<ElfData> Search(int anotherMemDiff = 0)
        {
            if (anotherMemDiff != 0)
                memDiff = anotherMemDiff;
            foreach (var entry in po.Entries)
            {
                Console.WriteLine($"\nSearching {entry.Original}...");
                SearchEntry(entry);
            }

            return data;
        }

        private void SearchEntry(PoEntry entry)
        {
            var pattern = findSequence(elf, 0, GetBytesFromString(entry.Original));
            var positionsLists = new List<int>();
            var withoutZero = false;
            var isFixed = false;

            if (pattern == -1)
            {
                pattern = findSequence(elf, 0, GetBytesFromString(entry.Original, true));
                if (pattern == -1)
                {
                    Console.WriteLine($"WARNING: The string: \"{entry.Original}\" is not found on the exe, skipping...");
                    return;
                }

                withoutZero = true;
            }

            // Search for the first time for knowing if is a fixed length entry or pointer based entry.
            var pointer = pattern + memDiff + (withoutZero ? 0 : 1);
            var textPointer = BitConverter.GetBytes(pointer);
            var result = findSequence(elf, 0, textPointer);


            switch (result)
            {
                // Not found, but is a fixed length entry.
                case -1 when containsFixedLengthEntries:
                    isFixed = true;
                    positionsLists.Add(pattern);
                    break;
                // Not found and the file doesn't contains fixed length entries.
                case -1 when !containsFixedLengthEntries:
                    Console.WriteLine($"WARNING: The string pointer \"{entry.Original}\" is not found on the exe, skipping...");
                    return;
                // Found.
                default:
                {
                    Console.WriteLine($"Found: 0x{result:X4}");
                    positionsLists.Add(result);
                    var currentPosition = result;

                    do
                    {
                        currentPosition = findSequence(elf, currentPosition + 1, textPointer);
                        if (currentPosition != -1)
                        {
                            Console.WriteLine($"Found: 0x{currentPosition:X4}");
                            positionsLists.Add(currentPosition);
                        }
                    } while (currentPosition != -1);

                    break;
                }
            }


            data.Add(new ElfData()
            {
                Text = entry.Translated,
                positions = positionsLists,
                FixedLength = isFixed
            });
        }


        private byte[] GetBytesFromString(string text, bool withoutZero = false)
        {
            var startData = withoutZero ? new List<byte>() : new List<byte>() { 0 };

            startData.AddRange(encoding.GetBytes($"{text}\0"));

            return startData.ToArray();
        }


        /// <summary>Looks for the next occurrence of a sequence in a byte array</summary>
        /// <param name="array">Array that will be scanned</param>
        /// <param name="start">Index in the array at which scanning will begin</param>
        /// <param name="sequence">Sequence the array will be scanned for</param>
        /// <returns>
        ///   The index of the next occurrence of the sequence of -1 if not found
        /// </returns>
        private int findSequence(byte[] array, int start, byte[] sequence)
        {
            int end = array.Length - sequence.Length; // past here no match is possible
            byte firstByte = sequence[0]; // cached to tell compiler there's no aliasing

            while (start <= end)
            {
                // scan for first byte only. compiler-friendly.
                if (array[start] == firstByte)
                {
                    // scan for rest of sequence
                    for (int offset = 1; ; ++offset)
                    {
                        if (offset == sequence.Length)
                        { // full sequence matched?
                            return start;
                        }
                        else if (array[start + offset] != sequence[offset])
                        {
                            break;
                        }
                    }
                }
                ++start;
            }

            // end of array reached without match
            return -1;
        }
    }
}
