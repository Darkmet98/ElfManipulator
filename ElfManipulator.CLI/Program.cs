using System;
using ElfManipulator.Functions;

namespace ElfManipulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Elfmanipulator CLI - A simple cli for the plugin ElfManipulator by Darkmet98.\nThanks to kaplas for all information for the executable\nThanks to pleonex for yarhl libraries.");
            if (args.Length == 0)
            {
                Usage();
                Environment.Exit(0);
            }

            switch (args[0].ToUpper())
            {
                case "-PATCH":
                    var e = new ApplyTranslation(args[1]);
                    e.GenerateElfPatched();
                    break;
                case "-INFO":
                    new ShowInfo(args[1]);
                    break;
                default:
                    Usage();
                    Environment.Exit(0);
                    break;
            }
            
            Console.WriteLine("Done.");
        }

        private static void Usage()
        {
            Console.WriteLine("USAGE:\n");
            Console.WriteLine("Patch the executable with a json file: \"ElfManipulator.CLI -patch example.json\"");
            Console.WriteLine("Write all executable info into txt: \"ElfManipulator.CLI -info example.exe\"");
        }
    }
}
