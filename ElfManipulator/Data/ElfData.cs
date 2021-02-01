using System.Collections.Generic;

namespace ElfManipulator.Data
{
    internal class ElfData
    {
        public List<int> positions { get; set; }
        public string Text { get; set; }
        public bool FixedLength { get; set; }

        public ElfData()
        {
            positions = new List<int>();
        }
    }
}
