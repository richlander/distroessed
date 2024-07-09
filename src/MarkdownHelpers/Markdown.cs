namespace MarkdownHelpers;

public class Markdown
{
    public static void WriteHeader(StreamWriter writer, string[] labels, int[] lengths)
    {
        if (lengths.Length > labels.Length)
        {
            throw new ArgumentException("Insufficent set of labels", nameof(labels));
        }

        for (int i = 0; i < lengths.Length; i++)
        {
            WriteColumn(writer, labels[i], lengths[i], i > 0);
        }

        writer.WriteLine();

        for (int i = 0; i < lengths.Length; i++)
        {
            WriteRepeatCharacter(writer, '-', lengths[i]);
            writer.Write("|");
        }

        writer.WriteLine();
    }

    public static void WriteColumn(StreamWriter writer, string label, int length, bool frontPad = true, char repeatCharacter = ' ')
    {
        if (frontPad)
        {
            writer.Write(' ');
        }

        writer.Write(label);
        if (label.Length < length)
        {
            int columnLength = frontPad ? length - label.Length - 1 : length - label.Length;
            WriteRepeatCharacter(writer, repeatCharacter, columnLength);
            writer.Write("|");
        }
        else
        {
            writer.Write(" |");
        }
    }

    private static void WriteRepeatCharacter(StreamWriter writer, char repeatCharacter, int repeats)
    {
        for (int i = 0; i < repeats; i++)
        {
            writer.Write(repeatCharacter);
        }
    }
}
