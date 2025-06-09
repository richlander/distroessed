using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MarkdownHelpers;

// This class is more complicated than one would like
// It all comes from offering both async and non-async "Processor" delegates
// And the conditional section concept
// This comes to mind: https://rachit.pl/post/you-have-built-a-compiler/
public class MarkdownTemplate
{
    private const string _openSymbol = "{{";
    private const string _closeSymbol = "}}";
    private const string _startSection = "-start";
    private const string _endSection = "-end";

    public Func<string, StreamWriter, Task>? AsyncProcessor { get; set; }

    public Action<string, StreamWriter>? Processor { get; set; }

    public Func<string, bool>? SectionProcessor { get; set; }

    public async Task ProcessAsync(StreamReader reader, StreamWriter writer)
    {
        String? line = "";
        // per line
        while ((line = reader.ReadLine()) != null)
        {
            bool inSection = false;

            // Sections of the template can be conditional
            if (IsSymbolLine(line, out string key))
            {
                if (IsConditionalLine(key))
                {
                    SetConditions(key, inSection, out inSection);
                    continue;
                }

                if (AsyncProcessor is null)
                {
                    throw new($"{nameof(AsyncProcessor)}  is null.");
                }

                // Block-level replacements can be async
                await AsyncProcessor(key, writer);
                continue;
            }
            else if (inSection)
            {
                continue;
            }

            // Inline replacements must be sync
            ProcessLine(writer, line);
        }
    }

    public void Process(StreamReader reader, StreamWriter writer)
    {
        String? line = "";
        bool inSection = false;
        // per line
        while ((line = reader.ReadLine()) != null)
        {

            // Sections of the template can be conditional
            if (IsSymbolLine(line, out string key))
            {
                if (IsConditionalLine(key))
                {
                    SetConditions(key, inSection, out inSection);
                    continue;
                }

                if (Processor is null)
                {
                    throw new($"{nameof(Processor)}  is null.");
                }

                Processor(key, writer);
                continue;
            }
            else if (inSection)
            {
                continue;
            }

            ProcessLine(writer, line);
        }
    }

    public void ProcessLine(StreamWriter writer, ReadOnlySpan<char> line)
    {
        while (line.Length > 0)
        {
            // look for next replacement text
            Replacement replacement = Replacement.FindNext(line);
            if (replacement.Found)
            {
                writer.Write(line[..replacement.StartIndex]);
            }
            else
            {
                writer.Write(line);
                break;
            }

            if (Processor is { })
            {
                Processor(replacement.Key, writer);
            }

            if (replacement.AfterIndex is -1)
            {
                break;
            }

            line = line[replacement.AfterIndex..];
        }

        writer.WriteLine();
    }

    private void SetConditions(string key, bool inConditionalSection, out bool isConditional)
    {
        if (key.EndsWith(_startSection))
        {
            isConditional = SectionProcessor is { } && !SectionProcessor(key);
        }
        else if (key.EndsWith(_endSection))
        {
            isConditional = false;
        }
        else
        {
            isConditional = inConditionalSection;
        }
    }

    private static bool IsSymbolLine(string line, out string key)
    {
        if (line.StartsWith(_openSymbol) && line.EndsWith(_closeSymbol))
        {
            if (FindNextReplacement(line, out int openIndex, out int afterIndex, out key))
            {
                if (line.Length == key.Length + 4)
                {
                    return true;
                }
            }
        }

        key = "";
        return false;
    }

    private static bool IsConditionalLine(string key) => key.EndsWith(_startSection) || key.EndsWith(_endSection);

    private static bool FindNextReplacement(ReadOnlySpan<char> line, out int openIndex, out int afterIndex, out string key)
    {
        openIndex = line.IndexOf(_openSymbol);

        if (openIndex is -1 ||
            !HasCloseIndex(line[openIndex..], out int closeIndex))
        {
            key = "";
            afterIndex = -1;
            return false;
        }

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
