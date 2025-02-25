using System.Text;

namespace MarkdownHelpers;

public class BreakBuffer(StringBuilder builder, int index = 0)
{
    readonly StringBuilder _builder = builder;
    int _index = index;

    public bool LinkFormat { get; set; } = false;

    public string BreakSequence { get; set; } = " ; ";

    public void Append(string value)
    {
        if (_index > 0)
        {
            _builder.Append(BreakSequence);
        }

        if (LinkFormat)
        {
            _builder.Append($"<{value}>");
        }
        else
        {
            _builder.Append(value);
        }
        _index++;
    }

    public void AppendRange(IEnumerable<string> values)
    {
        foreach (string value in values)
        {
            Append(value);
        }
    }

    public override string ToString()
    {
        return _builder.ToString();
    }
}