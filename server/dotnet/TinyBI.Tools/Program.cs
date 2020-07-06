using System;
using System.IO;
using System.Runtime.Loader;

namespace TinyBI.Tools
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 4 || args[0] != "ts")
            {
                Console.WriteLine("Usage: ts <dll> <schema-class> <ts-file>");
                return -1;
            }

            var path = Path.GetFullPath(args[1]);

            Console.WriteLine($"Reading assembly {path}");

            var schemaType = AssemblyLoadContext.Default.LoadFromAssemblyPath(path).GetType(args[2]);
            if (schemaType == null)
            {
                Console.WriteLine($"No such type {schemaType} in assembly");
                return -1;
            }

            Console.WriteLine($"Reading type {schemaType.FullName}");

            using var writer = new StreamWriter(args[3]);

            Console.WriteLine($"Saving to file {args[3]}");

            writer.WriteLine(@"import { QueryColumn } from ""tinybi"";");
            writer.WriteLine();

            static string MapColumnType(Type clrType) =>
                clrType == typeof(bool) ? "boolean" :
                clrType == typeof(DateTime) ? "Date" :
                clrType == typeof(string) ? "string" :
                "number";

            foreach (var table in new Schema(schemaType).Tables)
            {
                Console.WriteLine($"Exporting table {table.RefName}");
                writer.WriteLine($"export const {table.RefName} = {{");

                foreach (var column in table.Columns)
                {
                    var tsType = MapColumnType(column.ClrType);
                    writer.WriteLine(@$"    {column.RefName}: new QueryColumn<{tsType}>(""{table.RefName}.{column.RefName}""),");
                }

                writer.WriteLine("};");
                writer.WriteLine();
            }

            writer.Flush();
            Console.WriteLine("Done.");

            return 0;
        }
    }
}
