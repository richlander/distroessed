using System.Text;

namespace MarkdownHelpers;

public class StringBuilderIWriter(StringBuilder builder) : IWriter
{
    private readonly StringBuilder _sb = builder;

    public void Write(string value) => _sb.Append(value);

    public void Write(char value) => _sb.Append(value);

    public void WriteLine(string value) => _sb.Append(value);

    public void WriteLine() => _sb.AppendLine();
}

public class StreamWriterIWriter(StreamWriter writer) : IWriter
{
    private readonly StreamWriter _writer = writer;

    public void Write(string value) => _writer.Write(value);

    public void Write(char value) => _writer.Write(value);

    public void WriteLine(string value) => _writer.WriteLine(value);

    public void WriteLine() => _writer.WriteLine();
}

public class Writer
{
    public static IWriter GetWriter(StringBuilder builder) => new StringBuilderIWriter(builder);

    public static IWriter GetWriter(StreamWriter writer) => new StreamWriterIWriter(writer);

}
