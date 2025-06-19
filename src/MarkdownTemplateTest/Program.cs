using System.Runtime.InteropServices;
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
        else if (key == "os")
        {
            writer.Write(RuntimeInformation.OSDescription);
        }
        else if (key == "arch")
        {
            writer.Write(RuntimeInformation.OSArchitecture);
        }
        else if (key == "some-section")
        {
            foreach (var i in Enumerable.Range(1, 3))
            {
                writer.WriteLine($"- Item {i}");
            }
        }
        else if (key == "another-section")
        {
            writer.WriteLine("This is another section content.");
        }
        else if (key == "high-pri-content")
        {
            writer.WriteLine("This is some high-priority content.");
        }
        else if (key == "low-pri-content")
        {
            // This section will be skipped
            writer.WriteLine("Very low-priority content.");
        }
        else
        {
            Console.WriteLine($"Unknown key: {key}");
        }
        Console.WriteLine($"Finished processing key: {key}");
    },
    ShouldIncludeSection = (section) =>
    {
        Console.WriteLine($"Checking if section '{section}' should be included.");
        if (section == "high-pri-section")
        {
            return true; // Always include this section
        }
        else if (section == "low-pri-section")
        {
            return false; // Skip this section
        }
        return false;
    }
};

template.Process(reader, writer);
writer.Close();
Console.WriteLine(File.ReadAllText(outputPath));
