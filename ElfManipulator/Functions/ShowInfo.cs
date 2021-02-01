using System.IO;
using System.Text;
using AsmResolver.PE.File;

namespace ElfManipulator.Functions
{
    public class ShowInfo
    {
        public ShowInfo(string elfPath)
        {
            if (!File.Exists(elfPath))
                throw new FileNotFoundException("The executable file is not found.", elfPath);

            var pe = PEFile.FromFile(elfPath);

            var sb = new StringBuilder();

            sb.AppendLine($"Header info from executable {Path.GetFileName(elfPath)}\n\n");

            foreach (var section in pe.Sections)
            {
                sb.AppendLine($"SECTION NAME: {section.Name}");
                sb.AppendLine($"OFFSET: 0x{section.Offset:X}");
                sb.AppendLine($"RVA: 0x{section.Rva:X}");
                sb.AppendLine($"PHYSICAL SIZE: 0x{section.GetPhysicalSize():X}");
                sb.AppendLine($"VIRTUAL SIZE: 0x{section.GetVirtualSize():X}");
                var translationSectionBase = (int)(pe.OptionalHeader.ImageBase +
                                                   (section.Rva - section.Offset));
                sb.AppendLine($"TRANSLATION SECTION BASE: 0x{translationSectionBase:X}");
                sb.AppendLine($"IS READABLE: {section.IsReadable}");

                sb.AppendLine($"\n\n");
            }

            File.WriteAllText(elfPath + ".txt", sb.ToString());
        }
    }
}
