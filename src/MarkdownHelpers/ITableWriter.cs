namespace MarkdownHelpers;

public interface IWriter
{
    void Write(string value);

    void Write(char value);

    void WriteLine(string value);

    void WriteLine();
}