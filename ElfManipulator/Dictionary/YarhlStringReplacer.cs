using System;
using System.IO;
using Yarhl.IO;
using Yarhl.Media.Text;

namespace ElfManipulator.Dictionary
{
    public class YarhlStringReplacer
    {

        private readonly Replacer replacer;

        public YarhlStringReplacer()
        {
            replacer = new Replacer();
        }
        public void AddDictionary(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException();

            var openFile = new Yarhl.IO.TextReader(
                DataStreamFactory.FromFile(file, FileOpenMode.Read));

            if (openFile.Stream.Length == 0)
                throw new Exception("The file is empty.");

            var separatorLine = openFile.ReadLine();

            if (!separatorLine.Contains("separator="))
                throw new Exception("The separator has not been set.");

            if (separatorLine.Length != 11)
                throw new Exception("The separator not support multiple chars like == or ::");

            var separator = separatorLine[10];

            do
            {
                var line = openFile.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var textSplit = line.Split(separator);

                if (textSplit.Length != 2)
                    throw new Exception("The original or modified string contains the separator or is missing, please check your separator.");

                replacer.Add(textSplit[0], textSplit[1]);

            } while (!openFile.Stream.EndOfStream);

        }

        public string GetModified(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new Exception("The text is null.");

            return replacer.Map.Count == 0 ? text : replacer.TransformForward(text);
        }

        public string GetOriginal(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new Exception("The text is null.");

            return replacer.Map.Count == 0 ? text : replacer.TransformBackward(text);
        }
    }
}