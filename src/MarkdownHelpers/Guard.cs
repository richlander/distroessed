namespace MarkdownHelpers;

public class Guard(IWriter writer)
{
    private readonly IWriter _writer = writer;
    private int _indent = 0;

    public void StartRegion(string kind)
    {
        _writer.WriteLine($"```{kind}");
        _indent = 0;
    }

    public void EndRegion() => _writer.WriteLine("```");

    public void UpdateIndent(int indent) => _indent += indent;

    public void WriteLine(string value)
    {
        _writer.WriteRepeatCharacter(' ', _indent);
        _writer.WriteLine(value);
    }

}