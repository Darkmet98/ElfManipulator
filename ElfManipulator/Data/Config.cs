using System.Collections.Generic;

namespace ElfManipulator.Data
{
    /// <summary>
    /// Config for ElfManipulator
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Path for the executable.
        /// </summary>
        public string ElfPath { get; set; }
        /// <summary>
        /// Size of the new elf partition.
        /// </summary>
        public uint NewSize { get; set; }
        /// <summary>
        /// If the game contains fixed entries.
        /// </summary>
        public bool ContainsFixedEntries { get; set; }
        /// <summary>
        /// List with all po files and configs.
        /// </summary>
        public List<PoConfig> PoConfigs { get; set; }

        public Config()
        {
            PoConfigs = new List<PoConfig>();
        }
    }

    public class PoConfig
    {
        /// <summary>
        /// Path for po file.
        /// </summary>
        public string PoPath { get; set; }
        /// <summary>
        /// Section name to search on the executable.
        /// </summary>
        public string SectionName { get; set; }
        /// <summary>
        /// Encoding for the writer and mapping.
        /// </summary>
        public int EncodingId { get; set; }
    }
}
