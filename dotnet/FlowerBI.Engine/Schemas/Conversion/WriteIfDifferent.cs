using System;
using System.IO;
using System.Text;

namespace FlowerBI.Conversion;

internal class WriteIfDifferent : IDisposable
{
    private string _fileName;
    private TextWriter _console;

    public TextWriter Output { get; } = new StringWriter();
    public TextWriter Console { get; } = new StringWriter();

    public WriteIfDifferent(string fileName, TextWriter console)
    {
        _fileName = fileName;
        _console = console;
    }

    public void Dispose()
    {
        Output.Flush();
        Console.Flush();

        var newText = Output.ToString();

        if (File.Exists(_fileName))
        {
            var oldText = File.ReadAllText(_fileName);

            string RemoveCrs(string s) => s.Replace("\r", string.Empty);

            if (RemoveCrs(newText) == RemoveCrs(oldText))
            {
                return;
            }
        }

        _console.Write(Console.ToString());
        _console.WriteLine($"Saving to file {_fileName}");
        File.WriteAllText(_fileName, newText);
    }
}
