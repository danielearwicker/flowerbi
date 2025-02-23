using System;
using System.Reflection;
using FlowerBI.Conversion;

namespace FlowerBI.Tools;

internal class Program
{
    private static int Main(string[] args)
    {
        Console.WriteLine(
            $"FlowerBI Tools {Assembly.GetEntryAssembly().GetName().Version} running on dotnet {Environment.Version}"
        );

        if (args.Length == 4 && args[0] == "ts")
        {
            TypeScript.FromReflection(args[1], args[2], args[3], Console.Out);
        }
        else if (args.Length == 3 && args[0] == "ts")
        {
            TypeScript.FromYaml(args[1], args[2], Console.Out);
        }
        else if (args.Length == 4 && args[0] == "cs")
        {
            CSharp.FromYaml(args[1], args[2], args[3], Console.Out);
        }
        else if (args.Length == 4 && args[0] == "yaml")
        {
            Reflection.ToYaml(args[1], args[2], args[3], Console.Out);
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
