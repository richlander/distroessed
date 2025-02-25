using System.Threading.Tasks;

namespace MarkdownHelpers;

public class MarkdownTemplate
{
    private const string _openSymbol = "{{";
    private const string _closeSymbol = "}}";

    public async Task Process(StreamReader reader, StreamWriter writer, Func<string, StreamWriter, Task> action)
    {
        String? line = "";
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Contains(_openSymbol))
            {
                await ProcessReplacementLine(line, writer, action);
            }
            else
            {
                writer.WriteLine(line);
            }
        }
    }

    private async Task ProcessReplacementLine(string line, StreamWriter writer, Func<string, StreamWriter, Task> action)
    {
        int length = line.Length;
        while (FindNextReplacement(line, out int openIndex, out int closeIndex))
        {
            bool block = length == closeIndex + 2;
            var beforeOpen = line[..openIndex];
            var afterClose = line[(closeIndex + 2)..];
            var key = line.Substring(openIndex + 2, closeIndex - openIndex - 2);
            writer.Write(beforeOpen);
            await action(key, writer);

            if (block)
            {
                return;
            }   

            line = afterClose;
        }

        writer.WriteLine(line);
    }

    private bool FindNextReplacement(ReadOnlySpan<char> line, out int openIndex, out int closeIndex)
    {
        openIndex = line.IndexOf(_openSymbol);
        closeIndex = line.IndexOf(_closeSymbol);
        return openIndex != -1 && closeIndex != -1;
    }
}
