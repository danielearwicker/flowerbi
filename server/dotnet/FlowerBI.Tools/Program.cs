using System;
using System.IO;
using FlowerBI.Conversion;

namespace FlowerBI.Tools
{
    internal class Program
    {
        private static bool NeedToRun(string input, string output)
        {
            if (!File.Exists(output)) return true;

            var inputTime = File.GetLastWriteTimeUtc(input);
            var outputTime = File.GetLastWriteTimeUtc(output);
            return inputTime > outputTime;
        }

        private static int Main(string[] args)
        {
            if (args.Length == 4 && args[0] == "ts")
            {
                if (NeedToRun(args[1], args[3]))
                {
                    TypeScript.FromReflection(args[1], args[2], args[3], Console.Out);
                }
            }
            else if (args.Length == 3 && args[0] == "ts")
            {
                if (NeedToRun(args[1], args[2]))
                {
                    TypeScript.FromYaml(args[1], args[2], Console.Out);
                }
            }
            else if (args.Length == 4 && args[0] == "cs")
            {
                if (NeedToRun(args[1], args[2]))
                {
                    CSharp.FromYaml(args[1], args[2], args[3], Console.Out);
                }
            }
            else if (args.Length == 4 && args[0] == "yaml")
            {
                if (NeedToRun(args[1], args[3]))
                {
                    Reflection.ToYaml(args[1], args[2], args[3], Console.Out);
                }
            }
            else
            {
                Console.WriteLine("Usage:");
                Console.WriteLine();
                Console.WriteLine("Generate Typescript from a yaml declaration:");
                Console.WriteLine();
                Console.WriteLine("        ts <yaml-file> <ts-file>");
                Console.WriteLine();
                Console.WriteLine("Generate C# from a yaml declaration:");
                Console.WriteLine();
                Console.WriteLine("        cs <yaml-file> <cs-file> <cs-namespace>");
                Console.WriteLine();
                Console.WriteLine("Generate Typescript from an assembly:");
                Console.WriteLine();
                Console.WriteLine("        ts <dll> <schema-class> <ts-file>");
                Console.WriteLine();
                Console.WriteLine("Generate yaml from an assembly:");
                Console.WriteLine();
                Console.WriteLine("        yaml <dll> <schema-class> <yaml-file>");
                Console.WriteLine();
                return -1;
            }

            return 0;
        }
    }
}
