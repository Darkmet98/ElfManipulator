using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AsmResolver.PE.File;
using ElfManipulator.Data;
using Newtonsoft.Json;
using Yarhl.FileSystem;
using Yarhl.IO;
using Yarhl.Media.Text;

namespace ElfManipulator.Functions
{
    public class ApplyTranslation
    {
        private Config config;
        public ApplyTranslation(Config configPassed)
        {
            config = configPassed;
        }

        public ApplyTranslation(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("The json file is not found.", jsonPath);
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(jsonPath));
        }

        public void GenerateElfPatched()
        {
            var pe = ExpandFile();
            var mappings = GenerateMappings(pe);
            WriteContent(pe, mappings);
        }

        private void WriteContent(PEFile pe, List<ElfData[]> data)
        {
            var tradHeader = pe.Sections.FirstOrDefault(x => x.Name == ".trad");
            var memDiff = (int)(pe.OptionalHeader.ImageBase +
                                (tradHeader.Rva - tradHeader.Offset));

            var writer = new DataWriter(DataStreamFactory.FromStream(GenerateStream(pe)))
            {
                DefaultEncoding = Encoding.GetEncoding(config.EncodingId)
            };
            writer.Stream.Position = (long)tradHeader.Offset;
            

            foreach (var elfData in data.SelectMany(entry => entry))
            {
                if (elfData.FixedLength)
                {
                    writer.Stream.PushCurrentPosition();
                    writer.Stream.Position = elfData.positions[0];
                    writer.Write(elfData.Text);
                    writer.Stream.PopPosition();
                    continue;
                }

                var newPosition = (int)writer.Stream.Position + memDiff;
                writer.Write(elfData.Text);
                writer.Stream.PushCurrentPosition();
                foreach (var position in elfData.positions)
                {
                    writer.Stream.Position = position;
                    writer.Write(newPosition);
                }
                writer.Stream.PopPosition();
            }

            var newExe = (string.IsNullOrWhiteSpace(Path.GetDirectoryName(config.ElfPath)))
                ? string.Empty
                : Path.GetDirectoryName(config.ElfPath) + Path.DirectorySeparatorChar;
            writer.Stream.WriteTo(newExe + Path.GetFileNameWithoutExtension(config.ElfPath) + "_patched.exe");
        }

        private MemoryStream GenerateStream(PEFile pe)
        {
            var stream = new MemoryStream();
            pe.Write(stream);
            return stream;
        }

        private PEFile ExpandFile()
        {
            if (!File.Exists(config.ElfPath))
                throw new FileNotFoundException("The executable file is not found.", config.ElfPath);

            var expand = new Expand(config.ElfPath, config.NewSize);
            return expand.ExpandExe();
        }

        private List<ElfData[]> GenerateMappings(PEFile peFile)
        {
            var mappings = new List<ElfData[]>();

            if (!File.Exists(config.ElfPath))
                throw new FileNotFoundException("The executable file is not found.", config.ElfPath);

            var elfArray = File.ReadAllBytes(config.ElfPath);

            foreach (var configs in config.PoConfigs)
            {
                if (!File.Exists(configs.PoPath))
                    throw new FileNotFoundException("The po file is not found.", configs.PoPath);

                if (!peFile.Sections.ToList().Exists(x=>x.Name == configs.SectionName))
                    throw new Exception($"The elf section {configs.SectionName} is not found on the executable.");


                var translationSection = peFile.Sections.FirstOrDefault(x => x.Name == configs.SectionName);
                var memDiff = (int)(peFile.OptionalHeader.ImageBase +
                                    (translationSection.Rva - translationSection.Offset));

                var po = NodeFactory.FromFile(configs.PoPath).TransformWith(new Binary2Po()).GetFormatAs<Po>();
                var mapping = new GenerateMapping(elfArray, po, Encoding.GetEncoding(config.EncodingId), memDiff, config.ContainsFixedEntries);
                mappings.Add(mapping.Search().ToArray());
            }

            return mappings;
        }
    }
}
