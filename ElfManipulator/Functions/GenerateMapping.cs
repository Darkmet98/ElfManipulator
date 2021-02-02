using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ElfManipulator.Data;
using ElfManipulator.Dictionary;
using Yarhl.Media.Text;

namespace ElfManipulator.Functions
{
    public class GenerateMapping
    {
        private byte[] elf;
        private Po po;
        private List<ElfData> data;
        private Encoding encoding;
        private int memDiff;
        private bool containsFixedLengthEntries;
        private YarhlStringReplacer replacer;

        /// <summary>
        /// Generate a mapping with all parameters for patching the executable.
        /// </summary>
        /// <param name="elfOri">Byte array for the exe.</param>
        /// <param name="poPassed">Po file.</param>
        /// <param name="encodingPassed">Encoding for the reader.</param>
        /// <param name="memDiffPassed">Memory diff for searching the entries.</param>
        /// <param name="containsFixedLength">If the executable contains fixed length.</param>
        /// <param name="dictionaryPathPassed">The path of the dictionary.</param>
        /// <param name="customDictionary">If the game use a custom dictionary</param>
        public GenerateMapping(byte[] elfOri, Po poPassed, Encoding encodingPassed, int memDiffPassed, bool containsFixedLength, string dictionaryPathPassed, bool customDictionary = false)
        {
            elf = elfOri;
            po = poPassed;
            encoding = encodingPassed;
            memDiff = memDiffPassed;
            containsFixedLengthEntries = containsFixedLength;
            data = new List<ElfData>();
            replacer = new YarhlStringReplacer();

            // Check if the exe dictionary file exists.
            if (customDictionary)
                return;
            if (File.Exists(dictionaryPathPassed))
                replacer.AddDictionary(dictionaryPathPassed);
        }

        /// <summary>
        /// Initialize the pointers search.
        /// </summary>
        /// <returns>A mapped ElfData with all contents.</returns>
        public List<ElfData> Search()
        {
            foreach (var entry in po.Entries)
            {
                SearchEntry(entry);
            }

            return data;
        }

        /// <summary>
        /// Search the current entry.
        /// </summary>
        /// <param name="entry">PoEntry passed</param>
        private void SearchEntry(PoEntry entry)
        {
            // Instance the necessary data.
            var positionsLists = new List<int>();
            var withoutZero = false;
            var isFixed = false;
            var fixedEntrySize = 0;
            var textArray = encoding.GetBytes($"{entry.Original}\0");

            // Search the text. 
            var textLocation = findSequence(elf, 0, GetBytesFromString(textArray));

            // Not found, but try to search without the initial zero
            if (textLocation == -1)
            {
                // Search the text without initial zero.
                textLocation = findSequence(elf, 0, GetBytesFromString(textArray, true));
                
                // Not found.
                if (textLocation == -1)
                    throw new Exception($"The string: \"{entry.Original}\" is not found on the exe.");

                withoutZero = true;
            }

            // Search for the first time for knowing if is a fixed length entry or pointer based entry.
            var pointer = textLocation + memDiff + (withoutZero ? 0 : 1);

            // Get byte array from the pointer.
            var textPointer = BitConverter.GetBytes(pointer);

            // Search the pointer
            var result = findSequence(elf, 0, textPointer);


            switch (result)
            {
                // Not found, but is a fixed length entry.
                case -1 when containsFixedLengthEntries:
                    isFixed = true;
                    fixedEntrySize = textArray.Length - 1;
                    positionsLists.Add(textLocation);

                    // Generate the max length size.
                    var i = textLocation;
                    do
                    {
                        if (elf[i++] == 0)
                            fixedEntrySize++;
                        else
                            break;
                    } while (true);
                    break;
                // Not found and the file doesn't contains fixed length entries.
                case -1 when !containsFixedLengthEntries:
                    throw new Exception($" The string pointer \"{entry.Original}\" is not found on the exe.");
                // Found.
                default:
                {
                    positionsLists.Add(result);

                    // Get the result as current position.
                    var currentPosition = result;

                    do
                    {
                        currentPosition = findSequence(elf, currentPosition + 1, textPointer);

                        if (currentPosition != -1) 
                                positionsLists.Add(currentPosition);

                    } while (currentPosition != -1);

                    break;
                }
            }


            data.Add(new ElfData()
            {
                Text = UseDictionary(entry.Translated),
                Positions = positionsLists,
                FixedLength = isFixed,
                EncodingId = encoding.CodePage,
                SizeFixedLength = fixedEntrySize
            });
        }

        public virtual string UseDictionary(string text)
        {
            return replacer.GetModified(text);
        }

        /// <summary>
        /// Get bytes from the current text.
        /// </summary>
        /// <param name="text">Original text from the po entry.</param>
        /// <param name="withoutZero">If contains the initial zero.</param>
        /// <returns>Text byte array.</returns>
        private byte[] GetBytesFromString(byte[] text, bool withoutZero = false)
        {
            var startData = withoutZero ? new List<byte>() : new List<byte>() { 0 };

            startData.AddRange(text);

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
