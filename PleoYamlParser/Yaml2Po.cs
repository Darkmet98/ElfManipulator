using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Yarhl.FileFormat;
using Yarhl.IO;
using Yarhl.Media.Text;

namespace PleoYamlParser
{
    public class Yaml2Po : IConverter<BinaryFormat, Po>
    {
        StringDefinitionBlock block;
        public DataStream streamElf { get; set; }

        // Modified from here
        // https://github.com/pleonex/AttackFridayMonsters/blob/master/Programs/AttackFridayMonsters/AttackFridayMonsters.Formats/Text/Code/BinaryStrings2Po.cs
        public Po Convert(BinaryFormat source)
        {
            string yaml = new TextReader(source.Stream).ReadToEnd();
            block = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build()
                .Deserialize<StringDefinitionBlock>(yaml);
            
            Po po = new Po
            {
                Header = new PoHeader(
                    "Generic game",
                    "email@any.com",
                    "es-ES"),
            };

            DataReader reader = new DataReader(streamElf);
            
            foreach (var definition in block.Definitions)
            {
                reader.Stream.Position = definition.Address - block.Offset[0].Ram;
                var encoding = Encoding.GetEncoding(definition.Encoding);
                string text = reader.ReadString(definition.Size, encoding).Replace("\0", string.Empty);

                string pointers = string.Join(",", definition.Pointers?.Select(p => $"0x{p:X}") ?? Enumerable.Empty<string>());

                var entry = new PoEntry
                {
                    Original = text,
                    Context = $"0x{definition.Address:X8}",
                    Flags = "c-format",
                    Reference = $"0x{definition.Address:X}:{definition.Size}:{definition.Encoding}:{pointers}",
                };
                po.Add(entry);
            }

            return po;
        }
    }
}
