using System.IO;
using System.Text;

namespace FlowerBI.Conversion;

internal class IndentedWriter : TextWriter
{
    public TextWriter Inner { get; }

    private string _indent;

    public IndentedWriter(TextWriter inner, int size = 4)
    {
        Inner = inner;
        _indent = new string(' ', size);
    }

    public override Encoding Encoding => Inner.Encoding;

    public override void Write(char value) => Inner.Write(value);

    public override void WriteLine(string text) => Inner.WriteLine($"{_indent}{text}");
}
