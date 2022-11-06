using System.IO;

namespace FlowerBI.Tools;

internal static class TextWriterExtensions
{
    private const string indent = "    ";
    internal static void WriteIndentedLine(this TextWriter writer, string value, int indentation = 0)
    {
        for (int i = 0; i < indentation; i++)
        {
            writer.Write(indent);
        }
        writer.WriteLine(value);
    }
}