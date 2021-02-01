using System;
using ElfManipulator.Functions;

namespace ElfManipulator
{
    class Program
    {
        static void Main(string[] args)
        {
            //var a = new ShowInfo(args[0]);
            var e = new ApplyTranslation("ys1.json");
            e.GenerateElfPatched();
        }
    }
}
