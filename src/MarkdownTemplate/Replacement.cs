namespace MarkdownHelpers;

public ref struct Replacement
{
    private const string _openSymbol = "{{";

    private const string _closeSymbol = "}}";

    public bool Found { get; set; }

    public ReadOnlySpan<char> Key { get; set; }

    public Replacement(ReadOnlySpan<char> text)
    {

    }
    public static Replacement FindNextReplacement(ReadOnlySpan<char> line)
    {
        int openIndex = line.IndexOf(_openSymbol);
        int closeIndex = line[openIndex..].IndexOf(_closeSymbol);

        if (openIndex == -1 || closeIndex == -1)
        {
            return new Replacement { Found = false, Key = ReadOnlySpan<char>.Empty };
        }

        closeIndex += openIndex; // Adjust closeIndex to be absolute index in the line


        int keyEndIndex = openIndex + closeIndex;
        int keyStartIndex = openIndex + 2;
        key = line[keyStartIndex..keyEndIndex].ToString();
        int keyAfterIndex = keyEndIndex + _closeSymbol.Length;
        afterIndex = keyAfterIndex >= line.Length ? -1 : keyAfterIndex;
        return true;

        static bool HasCloseIndex(ReadOnlySpan<char> line, out int closeIndex)
        {
            closeIndex = line.IndexOf(_closeSymbol);
            return closeIndex > -1;
        }
    }
}