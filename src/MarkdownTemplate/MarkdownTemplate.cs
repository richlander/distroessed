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

        Other notes:
        - Algorithm is re-entrant (if that's the correct term).
        - The idea is that most of the logic should be in this method, leaving
          calling a sync or async lambda and very little duplicated code
          or complexity in the calling method.
        - Makes use of "" and "\n" to signal whether a newline is needed.
    */
    public (Replacement, string) ProcessLines(StreamReader reader, StreamWriter writer, string? line, Replacement lastReplacement)
    {
        // expectation is that line is initially null
        line ??= reader.ReadLine();
        // sections can be conditional; need a model to skip lines
        bool skipSection = false;
        while (true)
        {
            // if line is null, that means EOF
            if (line == null)
            {
                return (Replacement.None, string.Empty);
            }

            // various forms of empty lines
            if (line is "" or "\n")
            {
                // signal to emit newline
                if (line is "\n")
                {
                    writer.WriteLine();
                }

                line = reader.ReadLine(); // Read the next line
                line = !skipSection && line is "" ? writer.NewLine : line;
                continue;
            }
            
            // Look for replacement content
            Replacement replacement = Replacement.FindNext(line);
            // Commands indicate where lines are conditional
            // Could be extended to other scenarios
            bool hasCommands = replacement.Commands is {};

            // Plain text that should be skipped
            if (skipSection && !hasCommands)
            {
                line = "";
                continue;
            }

            // start or end of section found
            if (hasCommands)
            {
                var commands = replacement.Commands ?? [];
                bool start = commands.Contains("start");
                bool end = commands.Contains("end");

                if (start)
                {
                    // ask if this section is conditional / skipable
                    // default is yes
                    // ShouldIncludeSection only supports sync lambdas
                    skipSection = !(
                        ShouldIncludeSection is { } &&
                        ShouldIncludeSection(replacement.Key));
                }
                else if (skipSection && end)
                {
                    skipSection = false;
                }

                line = "";
                continue;
            }

            if (replacement.Found)
            {
                writer.Write(line[..replacement.StartIndex]);
                if (replacement.AfterIndex >= line.Length)
                {
                    line = writer.NewLine;
                }
                else
                {
                    line = line[replacement.AfterIndex..];
                }
                
                // return to get replacement
                return (replacement, line);

            }
            else
            {
                // continue to read file
                writer.WriteLine(line);
                line = "";
            }
        }
    }
}
