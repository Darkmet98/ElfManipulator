using AsmResolver;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;

namespace ElfManipulator.Functions
{
    internal class Expand
    {
        private uint newSize;
        private PEFile peFile;
        
        /// <summary>
        /// Expand the current executable for write the new content.
        /// </summary>
        /// <param name="path">Path of the executable.</param>
        /// <param name="size">Size of the new partition.</param>
        public Expand(string path, uint size)
        {
            newSize = size;
            peFile = PEFile.FromFile(path);
        }

        /// <summary>
        /// Expand the exe.
        /// </summary>
        /// <returns>Expanded executable.</returns>
        public PEFile ExpandExe()
        {
            CreateSection();
            return peFile;
        }

        /// <summary>
        /// Create the new section to the exe
        /// </summary>
        private void CreateSection()
        {
            // Create the new section.
            var section = new PESection(".trad", SectionFlags.MemoryRead | SectionFlags.ContentInitializedData);

            // Initialize the data
            var physicalContents = new DataSegment(new byte[newSize]);
            section.Contents = new VirtualSegment(physicalContents, newSize);

            // Add the new section into the exe.
            peFile.Sections.Add(section);

            // Update the exe headers.
            peFile.UpdateHeaders();
        }
    }
}
