using CveInfo;
using MarkdownHelpers;

// Format is inspired by https://security.alpinelinux.org/vuln/CVE-2024-5535
// Usage:
// CveMarkdown cve.json cve-template.md
// CveMarkdown ~/git/core/release-notes/cves/2025/01/cve.json ~/git/distroessed/templates/cve-template.md

// Static strings
const string jsonFilename = "cve.json";
const string targetFilename = "cve.md";
const string templateFilename = "cve-template.md";

Console.WriteLine("CveMarkdown");

if (args.Length is < 2 ||
   !(File.Exists(args[0]) && args[0].EndsWith(jsonFilename) &&
     File.Exists(args[1]) && args[1].EndsWith(templateFilename)))
{
    ReportInvalidArgs();
    return;
}

string source = args[0];
string template = args[1];
string directory = Path.GetDirectoryName(source)!;
string target = Path.Combine(directory, targetFilename);

using var templateStream = File.OpenRead(template);
using var templateReader = new StreamReader(templateStream);
using var targetStream = File.Open(target, FileMode.Create);
using var targetWriter = new StreamWriter(targetStream);

using var jsonStream = File.OpenRead(source);
var cves = await CveSerializer.GetCveRecords(jsonStream);

if (cves?.Records is null)
{
    Console.WriteLine("JSON deserialization failed");
    return;
}

CveReport cveReport = new();
MarkdownTemplate notes = cveReport.CreateTemplate(cves);
notes.Process(templateReader, targetWriter);
cveReport.MakeMarkdownLinks(targetWriter);

Console.WriteLine($"Source: {source}");
Console.WriteLine($"Destination: {target}");

// Close file
Close(targetWriter, target);

static void Close(StreamWriter writer, string file)
{
    writer.Close();
    var writtenFile = File.OpenRead(file);
    long length = writtenFile.Length;
    string path = writtenFile.Name;
    writtenFile.Close();

    Console.WriteLine($"Generated {length} bytes");
    Console.WriteLine(path);
}

static void ReportInvalidArgs()
{
    Console.WriteLine("Invalid args.");
    Console.WriteLine("Usage: CveMarkdown path-to-cves.json [target-file]");
}
