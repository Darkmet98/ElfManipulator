using System;
using System.IO;
using Yarhl.FileSystem;
using Yarhl.Media.Text;

namespace PleoYamlParser
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PleoYamlParser - A simple yaml's pleo format parser to po.");
            if (args.Length < 2)
            {
                Console.WriteLine("USAGE: PleoYamlParser file.yaml file.exe");
                return;
            }

            if (!File.Exists(args[0]) && !File.Exists(args[1]))
                throw new FileNotFoundException();
            
            
            var node = NodeFactory.FromFile(args[0]);
            node.TransformWith(new Yaml2Po()
            {
                streamElf = NodeFactory.FromFile(args[1]).Stream
            }).TransformWith(new Po2Binary()).Stream.WriteTo(args[0].Replace(".yaml", ".po"));
        }
    }
}
