using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MarkdownHelpers;

// This class is more complicated than one would like
// It all comes from offering both async and non-async "Processor" delegates
// And the conditional section concept
// This comes to mind: https://rachit.pl/post/you-have-built-a-compiler/
public class MarkdownTemplate
{
    public Func<string, StreamWriter, Task>? AsyncProcessor { get; set; }

    public Action<string, StreamWriter>? Processor { get; set; }

    public Func<string, bool>? ShouldIncludeSection { get; set; }

    public async Task ProcessAsync(StreamReader reader, StreamWriter writer)
    {
        string? line = null;
        Replacement replacement = Replacement.None;

        while (true)
        {
            (replacement, line) = ProcessLines(reader, writer, line, replacement);

            if (replacement.Found)
            {
                if (AsyncProcessor is { })
                {
                    await AsyncProcessor(replacement.Key, writer);
                }
            }
            else
            {
                // No more replacements found, break the loop
                break;
            }
        }
    }

    public void Process(StreamReader reader, StreamWriter writer)
    {
        string? line = null;
        Replacement replacement = Replacement.None;

        while (true)
        {
            (replacement, line) = ProcessLines(reader, writer, line, replacement);

            if (replacement.Found)
            {
                if (Processor is { })
                {
                    Processor(replacement.Key, writer);
                }
            }
            else
            {
                // No more replacements found, break the loop
                break;
            }
        }
    }

    /*
        Cases to consider:
        - Text
        - Text with embedded replacements
        - Replacement line
        - Replacement line with commands
    */
    public (Replacement, string) ProcessLines(StreamReader reader, StreamWriter writer, string? line, Replacement lastReplacement)
    {
        bool skipSection = false;
        int index = lastReplacement.AfterIndex;

        // Signal that a line ended, requiring a newline
        if (line == string.Empty)
        {
            writer.WriteLine();
        }

        // Cases where a new source line is needed.
        if (line == null || line == string.Empty)
        {
            line = reader.ReadLine();
        }

        int startIndex = lastReplacement.StartIndex;
        if (startIndex > 0 && line?.Length > startIndex)
        {
            line = line[lastReplacement.AfterIndex..];
        }

        while (true)
        {
            if (line == null)
            {
                Console.WriteLine("No line to process.");
                return (Replacement.None, string.Empty);
            }

            if (line.Length is 0)
            {
                writer.WriteLine();
                line = reader.ReadLine(); // Read the next line
                continue;
            }

            bool isReplacementLine = Replacement.IsReplacementLine(line);

            if (skipSection && !isReplacementLine)
            {
                Console.WriteLine($"Skipping line: {line.ToString()}; line length: {line.Length}; (skipSection: {skipSection})");
                line = "";
                continue;
            }

            Console.WriteLine($"Processing line: {line.ToString()}; line length: {line.Length}; (skipSection: {skipSection}");
            Replacement replacement = Replacement.FindNext(line);
            Console.WriteLine($"Replacement found: {replacement.Found}, Key: {replacement.Key}, StartIndex: {replacement.StartIndex}, AfterIndex: {replacement.AfterIndex}");
            Console.WriteLine("Replacement Commands: " + (replacement.Commands != null ? string.Join(", ", replacement.Commands) : "null") + $"; Count: {replacement.Commands?.Length ?? 0}");

            if (isReplacementLine && replacement.Commands != null)
            {
                Console.WriteLine($"Processing symbol line A: {replacement.Key} with commands: {string.Join(", ", replacement.Commands)}");
                Console.WriteLine($"Processing symbol line B: {replacement.Key}; skipSection: {skipSection}");

                bool start = replacement.Commands.Contains("start");
                bool end = replacement.Commands.Contains("end");
                if (start)
                {
                    skipSection = !(ShouldIncludeSection is { } &&
                    ShouldIncludeSection(replacement.Key));
                }
                else if (skipSection && end)
                {
                    skipSection = false;
                }
                // skipSection =
                //     !(start &&
                //     ShouldIncludeSection is { } &&
                //     ShouldIncludeSection(replacement.Key)) || !(skipSection && end);
                line = "";
                Console.WriteLine($"Processing symbol line C: {replacement.Key}; skipSection: {skipSection}");
                continue;
                // if (start || end)
                // {
                //     Console.WriteLine("Breaking out of loop for start/end commands");
                //     continue;
                // }
            }
            else if (isReplacementLine && skipSection)
            {
                line = "";
                continue;
            }

            Console.WriteLine($"Processing segment: {line};  (skipSection: {skipSection})");

            if (replacement.Found)
            {
                writer.Write(line[..replacement.StartIndex]);
                if (replacement.AfterIndex >= line.Length)
                {
                    Console.WriteLine("Replacement AfterIndex is beyond line length, returning empty string.");
                    line = string.Empty;
                }
                return (replacement, line);

            }
            else
            {
                writer.Write(line);
                line = string.Empty;
            }

            // Console.WriteLine($"After processing segment: {line};  (skipSection: {skipSection})");
            // line = line[replacement.AfterIndex..];
        }

        return (Replacement.None, string.Empty);
    }
}
