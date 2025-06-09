using MarkdownHelpers;

string templatePath = args[0];
string outputPath = "output.md";

using var reader = new StreamReader(templatePath);
using var writer = new StreamWriter(outputPath);

MarkdownTemplate template = new MarkdownTemplate
{
    Processor = (key, writer) =>
    {
        // writer.WriteLine($"Processed sync: {key}");
        if (key == "date")
        {
            writer.Write($"{DateTime.Now:yyyy-MM-dd}");
        }
        else if (key == "some-section")
        {
            foreach(var i in Enumerable.Range(1, 3))
            {
                writer.WriteLine($"- Item {i}");
            }
        }
    }
};

template.Process(reader, writer);
