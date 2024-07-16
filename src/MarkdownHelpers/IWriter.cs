namespace MarkdownHelpers;

public interface IWriter
{
    void Write(string value);

    void Write(char value);

    void WriteLine(string value);

    void WriteLine();

    public void WriteRepeatCharacter(char repeatCharacter, int repeats)
    {
        for (int i = 0; i < repeats; i++)
        {
            Write(repeatCharacter);
        }
    }
}
