namespace MarkdownHelpers;

public ref struct Replacement
{
    private const string _openSymbol = "{{";

    private const string _closeSymbol = "}}";

    public bool Found { get; init; }

    public string Key { get; init; }

    public int StartIndex { get; init; }

    public int AfterIndex { get; init; }

    public static Replacement FindNext(ReadOnlySpan<char> line)
    {
        // Assuming: "{{key}} stuff"
        // Index at "{{"
        int replacementStart = line.IndexOf(_openSymbol);
        int replacementCloseCount = line[replacementStart..].IndexOf(_closeSymbol);

        if (replacementStart == -1 || replacementCloseCount == -1)
        {
            return new Replacement { Found = false, Key = string.Empty };
        }

        // index at "k"
        int keyStartIndex = replacementStart + _openSymbol.Length;
        // index at "y"
        int keyEndCount = replacementStart + replacementCloseCount;
        // index after "}}"
        int postIndex = keyEndCount + _closeSymbol.Length;
        int afterIndex = postIndex == line.Length ? line.Length - 1 : postIndex;
        Console.WriteLine($"Line: {line.ToString()}");
        Console.WriteLine($"Line length: {line.Length}");
        Console.WriteLine($"Replacement found at {replacementStart}, key starts at {keyStartIndex}, ends at {keyEndCount}, after index: {afterIndex}");
        // Console.WriteLine($"KeyStartIndex: {line[keyStartIndex..1]}, KeyEndCount: {line[keyEndCount..1]}");
        string key = line[keyStartIndex..keyEndCount].ToString();
        Console.WriteLine($"Key: {key}");
        return new Replacement { Found = true, Key = key, StartIndex = replacementStart, AfterIndex = afterIndex };
    }
}
