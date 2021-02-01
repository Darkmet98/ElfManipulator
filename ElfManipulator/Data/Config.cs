using System.Collections.Generic;

namespace ElfManipulator.Data
{
    public class Config
    {
        public string ElfPath { get; set; }
        public int EncodingId { get; set; }
        public uint NewSize { get; set; }
        public bool ContainsFixedEntries { get; set; }
        public List<PoConfig> PoConfigs { get; set; }

        public Config()
        {
            PoConfigs = new List<PoConfig>();
        }
    }

    public class PoConfig
    {
        public string PoPath { get; set; }
        public string SectionName { get; set; }
    }
}
