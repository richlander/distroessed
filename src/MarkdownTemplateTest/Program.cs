using MarkdownHelpers;

string templatePath = "/Users/rich/git/distroessed/src/MarkdownTemplateTest/template.md";//args[0];
string outputPath = "output.md";

using var reader = new StreamReader(templatePath);
using var writer = new StreamWriter(outputPath);

MarkdownTemplate template = new MarkdownTemplate
{
    Processor = (key, writer) =>
    {
        Console.WriteLine($"Lambda processing key: {key}");
        if (key == "date")
        {
            writer.Write($"{DateTime.Now:yyyy-MM-dd}");
        }
        else if (key == "some-section")
        {
            foreach (var i in Enumerable.Range(1, 3))
            {
                writer.WriteLine($"- Item {i}");
            }
        }
        else if (key == "section-content")
        {
            writer.WriteLine("This is some section content.");
        }
        else
        {
            writer.WriteLine($"Unknown key: {key}");
        }
        Console.WriteLine($"Finished processing key: {key}");
    }
};

template.Process(reader, writer);
writer.Close();
Console.WriteLine(File.ReadAllText(outputPath));
