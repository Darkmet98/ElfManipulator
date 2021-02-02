using System.Collections.Generic;

namespace ElfManipulator.Data
{
    /// <summary>
    /// Elf data entry to replace with the program.
    /// </summary>
    public class ElfData
    {
        /// <summary>
        /// List of positions to replace the current pointer.
        /// </summary>
        public List<int> Positions { get; set; }
        /// <summary>
        /// Text to write into the game.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Is a fixed length entry.
        /// </summary>
        public bool FixedLength { get; set; }
        /// <summary>
        /// Max size of the fixed entry.
        /// </summary>
        public int SizeFixedLength { get; set; }
        /// <summary>
        /// Encoding for the writer.
        /// </summary>
        public int EncodingId { get; set; }

        public ElfData()
        {
            Positions = new List<int>();
        }
    }
}
