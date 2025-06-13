namespace MarkdownHelpers;

public struct Replacement
{
    public const string OpenSymbol = "{{";
    public const string CloseSymbol = "}}";
    
    public bool Found { get; init; }

    public string Key { get; init; }

    public string[]? Commands { get; init; }

    public int StartIndex { get; init; }

    public int AfterIndex { get; init; }

    public static Replacement FindNext(ReadOnlySpan<char> line)
    {
        // Assuming: "{{key}} stuff"
        // Index at "{{"
        int replacementStart = line.IndexOf(OpenSymbol);
        int replacementCloseCount = replacementStart == -1
            ? -1
            : line[replacementStart..].IndexOf(CloseSymbol);

        if (replacementStart == -1 || replacementCloseCount == -1)
        {
            return new Replacement { Found = false, Key = string.Empty, StartIndex = 0, AfterIndex = line.Length };
        }

        // index at "k"
        int keyStartIndex = replacementStart + OpenSymbol.Length;
        // index at "y"
        int keyEndCount = replacementStart + replacementCloseCount;
        // index after "}}"
        int postIndex = keyEndCount + CloseSymbol.Length;
        int afterIndex = postIndex == line.Length ? line.Length : postIndex;
        var inner = line[keyStartIndex..keyEndCount];
        int index = inner.IndexOf(':');
        string key = index == -1
            ? inner.ToString()
            : inner[..index].ToString();

        string[]? commands = index > -1
            ? inner[(index + 1)..].ToString().Split(' ')
            : null;

        return new Replacement {
            Found = true,
            Key = key,
            StartIndex = replacementStart,
            AfterIndex = afterIndex,
            Commands = commands };
    }

    public static bool IsSymbolLine(ReadOnlySpan<char> line)
    {
        return line.StartsWith(OpenSymbol) && line.EndsWith(CloseSymbol);
    }
}
