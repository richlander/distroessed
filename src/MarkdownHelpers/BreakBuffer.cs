using System.Text;

namespace MarkdownHelpers;

public class BreakBuffer(StringBuilder builder, int index = 0)
{
    StringBuilder _builder = builder;
    int _index = index;

    public void Append(string value)
    {
        if (_index > 0)
        {
            _builder.Append("<br>");
        }

        _builder.Append(value);
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