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
        String? line = "";
        bool skipSection = false;
        while ((line = reader.ReadLine()) != null)
        {

            // Sections of the template can be conditional
            ProcessSymbolLine(line, skipSection, out skipSection);
            if (skipSection)
            {
                continue;
            }

            while (line.Length > 0)
            {
                Replacement replacement = ProcessLineNext(writer, line);
                line = line[replacement.AfterIndex..];
                if (!replacement.Found)
                {
                    break;
                }

                if (AsyncProcessor is { })
                {
                    await AsyncProcessor(replacement.Key, writer);
                }
            }

            writer.WriteLine();
        }
    }

    public void Process(StreamReader reader, StreamWriter writer)
    {
        ReadOnlySpan<char> line = "";
        bool skipSection = false;
        while ((line = reader.ReadLine()) != null)
        {

            // Sections of the template can be conditional
            ProcessSymbolLine(line, skipSection, out skipSection);
            if (skipSection)
            {
                continue;
            }

            while (line.Length > 0)
            {
                Replacement replacement = ProcessLineNext(writer, line);

                if (replacement.Found && Processor is { })
                {
                    Processor(replacement.Key, writer);
                }

                line = line[replacement.AfterIndex..];
            }

            writer.WriteLine();
        }
    }

    public void ProcessSymbolLine(ReadOnlySpan<char> line, bool inSkipSection, out bool afterSkipSection)
    {
        afterSkipSection = inSkipSection;
        if (Replacement.IsSymbolLine(line))
        {
            Replacement replacement = Replacement.FindNext(line);

            if (!replacement.Found)
            {
                throw new Exception($"Invalid replacement line: {line}");
            }

            string shortKey = Replacement.GetShortKey(replacement.Key);

            if (line.EndsWith(Replacement.StartSectionSuffix))
            {
                afterSkipSection = !(ShouldIncludeSection is { } && ShouldIncludeSection(shortKey));
            }
            else if (line.EndsWith(Replacement.EndSectionSuffix))
            {
                afterSkipSection = false;
            }
        }
    }

    public static Replacement ProcessLineNext(StreamWriter writer, ReadOnlySpan<char> line)
    {
        Replacement replacement = Replacement.FindNext(line);
        if (replacement.Found)
        {
            writer.Write(line[..replacement.StartIndex]);
        }
        else
        {
            writer.Write(line);
        }

        return replacement;
    }
}
