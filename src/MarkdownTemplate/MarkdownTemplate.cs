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

    // public async Task ProcessAsync(StreamReader reader, StreamWriter writer)
    // {
    //     String? line = "";
    //     bool skipSection = false;
    //     while ((line = reader.ReadLine()) != null)
    //     {

    //         Replacement replacement = GetNextReplacementForLine(writer, line);

    //         ProcessSymbolLine(line, skipSection, out bool newSkipSection);
    //         if (skipSection || newSkipSection)
    //         {
    //             skipSection = newSkipSection;
    //             continue;
    //         }

    //         skipSection = newSkipSection;

    //         while (line.Length > 0)
    //         {
    //             line = line[replacement.AfterIndex..];
    //             if (!replacement.Found)
    //             {
    //                 break;
    //             }

    //             if (AsyncProcessor is { })
    //             {
    //                 await AsyncProcessor(replacement.Key, writer);
    //             }
    //         }

    //         writer.WriteLine();
    //     }
    // }

    public void Process(StreamReader reader, StreamWriter writer)
    {
        ReadOnlySpan<char> line = "";
        bool skipSection = false;
        while ((line = reader.ReadLine()) != null)
        {
            bool isSymbolLine = Replacement.IsSymbolLine(line);
            bool afterSkipSection = false;

            while (line.Length > 0)
            {
                Console.WriteLine($"Processing line: {line.ToString()}; line length: {line.Length}; (skipSection: {skipSection}, afterSkipSection: {afterSkipSection})");
                Replacement replacement = Replacement.FindNext(line);
                Console.WriteLine($"Replacement found: {replacement.Found}, Key: {replacement.Key}, StartIndex: {replacement.StartIndex}, AfterIndex: {replacement.AfterIndex}");
                Console.WriteLine("Replacement Commands: " + (replacement.Commands != null ? string.Join(", ", replacement.Commands) : "null") + $"; Count: {replacement.Commands?.Length ?? 0}");

                if (isSymbolLine && replacement.Commands != null)
                {
                    Console.WriteLine($"Processing symbol line A: {replacement.Key} with commands: {string.Join(", ", replacement.Commands)}");
                    Console.WriteLine($"Processing symbol line B: {replacement.Key}; skipSection: {skipSection}, afterSkipSection: {afterSkipSection}");
                    skipSection = replacement.Commands.Contains("start");
                    afterSkipSection = replacement.Commands.Contains("end");
                    // false if afterSkipSection is true and skipSection is false
                    // skipSection = afterSkipSection ? false : skipSection;
                    Console.WriteLine($"Processing symbol line C: {replacement.Key}; skipSection: {skipSection}, afterSkipSection: {afterSkipSection}");
                    line = "";
                }

                Console.WriteLine($"Processing segment: {line};  (skipSection: {skipSection}, afterSkipSection: {afterSkipSection})");

                if (skipSection || afterSkipSection)
                {
                    Console.WriteLine($"Skipping line: {line}");
                    break;
                }

                if (replacement.Found)
                {
                    writer.Write(line[..replacement.StartIndex]);

                    if (Processor is { })
                    {
                        Processor(replacement.Key, writer);
                    }
                }
                else
                {
                    writer.Write(line);
                }


                Console.WriteLine($"After processing segment: {line};  (skipSection: {skipSection}, afterSkipSection: {afterSkipSection})");
                line = line[replacement.AfterIndex..];
            }

            if (!isSymbolLine)
            {
                writer.WriteLine();
            }
            
        }
    }
}
