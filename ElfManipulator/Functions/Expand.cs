using AsmResolver;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;

namespace ElfManipulator.Functions
{
    internal class Expand
    {
        private uint newSize;
        private PEFile peFile;
        public Expand(string path, uint size)
        {
            newSize = size;
            peFile = PEFile.FromFile(path);
        }

        public PEFile ExpandExe()
        {
            CreateSection();
            return peFile;
        }

        private void CreateSection()
        {
            var section = new PESection(".trad", SectionFlags.MemoryRead | SectionFlags.ContentInitializedData);
            var physicalContents = new DataSegment(new byte[newSize]);
            section.Contents = new VirtualSegment(physicalContents, newSize);

            peFile.Sections.Add(section);
            peFile.UpdateHeaders();
        }
    }
}
