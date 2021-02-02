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
        /// <summary>
        /// Apply the translation into the executable with a config parameter.
        /// </summary>
        /// <param name="configPassed">Config parameter.</param>
        public ApplyTranslation(Config configPassed)
        {
            config = configPassed;
        }

        /// <summary>
        /// Apply the translation into the executable with a json file for the config.
        /// </summary>
        /// <param name="jsonPath">Path to the json.</param>
        public ApplyTranslation(string jsonPath)
        {
            // Check if the file exists.
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("The json file is not found.", jsonPath);

            // Deserialize the json into the config.
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(jsonPath));
        }

        /// <summary>
        /// Generate the elf patched.
        /// </summary>
        public void GenerateElfPatched()
        {
            var pe = ExpandFile();
            WriteContent(pe, GenerateMappings(pe));
        }

        /// <summary>
        /// Write the translated content into the new partition.
        /// </summary>
        /// <param name="pe">Executable PEFile.</param>
        /// <param name="data">IEnumerable of ElfData array.</param>
        public void WriteContent(PEFile pe, IEnumerable<ElfData[]> data)
        {
            // Get the new header and calculate the absolute position.
            var tradHeader = pe.Sections.First(x => x.Name == ".trad");
            var memDiff = (int)(pe.OptionalHeader.ImageBase +
                                (tradHeader.Rva - tradHeader.Offset));

            // Initialize the writer.
            var writer = new DataWriter(DataStreamFactory.FromStream(GenerateStream(pe)))
            {
                Stream = {Position = (long) tradHeader.Offset}
            };


            foreach (var elfData in data.SelectMany(entry => entry))
            {
                // If a fixed length entry, only 
                if (elfData.FixedLength)
                {
                    // Convert the text into a byte array.
                    var textArray = Encoding.GetEncoding(elfData.EncodingId).GetBytes(elfData.Text);

                    // Check the size
                    if (elfData.SizeFixedLength != 0 && textArray.Length > elfData.SizeFixedLength)
                    {
                        throw new Exception($"The size of the fixed length entry \"{elfData.Text}\" is higher than the original.\n" +
                                            $"Max length: {elfData.SizeFixedLength}\n" +
                                            $"Current length: {textArray.Length}");
                    }
                    // Push current position.
                    writer.Stream.PushCurrentPosition();

                    // Go into the position.
                    writer.Stream.Position = elfData.Positions[0];

                    // Write the text.
                    writer.Write(textArray);

                    // Return to the previous position.
                    writer.Stream.PopPosition();

                    continue;
                }

                // Get the new position.
                var newPosition = (int)writer.Stream.Position + memDiff;

                // Write the text.
                writer.Write(elfData.Text, true, Encoding.GetEncoding(elfData.EncodingId));

                // Push the current position.
                writer.Stream.PushCurrentPosition();

                // Go to the positions that contains the text position and update.
                foreach (var position in elfData.Positions)
                {
                    writer.Stream.Position = position;
                    writer.Write(newPosition);
                }

                // Return to the previous position.
                writer.Stream.PopPosition();
            }

            // Get the current path of the original exe.
            var newExe = (string.IsNullOrWhiteSpace(Path.GetDirectoryName(config.ElfPath)))
                ? string.Empty
                : Path.GetDirectoryName(config.ElfPath) + Path.DirectorySeparatorChar;

            // Write the new exe.
            writer.Stream.WriteTo(newExe + Path.GetFileNameWithoutExtension(config.ElfPath) + "_patched.exe");
        }

        /// <summary>
        /// Generate a new stream from the modified executable.
        /// </summary>
        /// <param name="pe">Executable PEFile.</param>
        /// <returns>MemoryStream with the modified executable.</returns>
        private MemoryStream GenerateStream(PEFile pe)
        {
            var stream = new MemoryStream();
            pe.Write(stream);
            return stream;
        }

        /// <summary>
        /// Expand the executable for write the new content.
        /// </summary>
        /// <returns>A PEFile with the executable expanded.</returns>
        private PEFile ExpandFile()
        {
            // Check if the exe exists.
            if (!File.Exists(config.ElfPath))
                throw new FileNotFoundException("The executable file is not found.", config.ElfPath);

            var expand = new Expand(config.ElfPath, config.NewSize);
            return expand.ExpandExe();
        }

        /// <summary>
        /// Generate a List of mapping arrays for patching the game.
        /// </summary>
        /// <param name="peFile">Executable PEFile.</param>
        /// <returns>A list with all data for patch the executable.</returns>
        private IEnumerable<ElfData[]> GenerateMappings(IPEFile peFile)
        {
            var mappings = new List<ElfData[]>();

            // Check if the exe exists.
            if (!File.Exists(config.ElfPath))
                throw new FileNotFoundException("The executable file is not found.", config.ElfPath);

            // Load all original executable into a byte array.
            var elfArray = File.ReadAllBytes(config.ElfPath);

            foreach (var configs in config.PoConfigs)
            {
                // Check if the po exists.
                if (!File.Exists(configs.PoPath))
                    throw new FileNotFoundException("The po file is not found.", configs.PoPath);

                // Check if the exe section exists.
                if (!peFile.Sections.ToList().Exists(x=>x.Name == configs.SectionName))
                    throw new Exception($"The elf section {configs.SectionName} is not found on the executable.");

                // Get the absolute position from the specified exe section.
                var translationSection = peFile.Sections.First(x => x.Name == configs.SectionName);
                var memDiff = (int)(peFile.OptionalHeader.ImageBase +
                                    (translationSection.Rva - translationSection.Offset));

                // Load the po file.
                var po = NodeFactory.FromFile(configs.PoPath).TransformWith(new Binary2Po()).GetFormatAs<Po>();

                

                mappings.Add(GenerateCustomMapping(elfArray, po, configs.EncodingId, memDiff, config.ContainsFixedEntries, configs.DictionaryPath, configs.CustomDictionary));
            }

            return mappings;
        }

        public virtual ElfData[] GenerateCustomMapping(byte[] elfArray, Po po, int encoding, int memDiff, bool containsFixedEntries, string dictionaryPath, bool customDictionary=false)
        {
            // Pass all arguments and generate the mapping.
            var mapping = new GenerateMapping(elfArray, po, Encoding.GetEncoding(encoding), memDiff, containsFixedEntries, dictionaryPath, customDictionary);
            return mapping.Search().ToArray();
        }
    }
}
