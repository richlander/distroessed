
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
   !File.Exists(args[1]) && args[1].EndsWith(templateFilename))
{
    ReportInvalidArgs();
    return;
}

string source = args[0];
string template = args[1];

if (source.EndsWith(jsonFilename))
{
    var report = await CveReport.MakeReport(source, targetFilename, template);
    return;
}
else if (Directory.Exists(source))
{
    var report = await CveReport.MakeReportForDirectory(source, jsonFilename, targetFilename, template);
    return;
}
else
{
    Console.WriteLine($"Source '{source}' is not a valid file or directory.");
    ReportInvalidArgs();
    return;
}

static void ReportInvalidArgs()
{
    Console.WriteLine("Invalid args.");
    Console.WriteLine("Usage: CveMarkdown path-to-cves.json [target-file]");
}
