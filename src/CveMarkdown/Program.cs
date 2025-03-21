using CveInfo;
using CveReport;
using MarkdownHelpers;

Console.WriteLine("CveMarkdown");

if (args.Length is 0)
{
    ReportInvalidArgs();
    return;
}

string json = "cves.json";
string targetFile = "cves.md";

if (File.Exists(args[0]))
{
    json = args[0];

    if (args.Length > 1)
    {
        if (Directory.Exists(args[1]))
        {
            targetFile = Path.Combine(args[1], targetFile);
        }
        else
        {
            targetFile = args[1];
        }
    }
}
else if (Directory.Exists(args[0]))
{
    json = Path.Combine(args[0], json);
    targetFile = Path.Combine(args[0], targetFile);
}
else
{
    ReportInvalidArgs();
    return;
}

Console.WriteLine($"Source: {json}");
Console.WriteLine($"Destination: {targetFile}");

var jsonStream = File.OpenRead(json);
var cves = await Cves.GetCves(jsonStream);

if (cves?.Cves is null)
{
    Console.WriteLine("JSON deserialization failed");
    return;
}

var stream = File.Open(targetFile, FileMode.Create);
var writer = new StreamWriter(stream);

// Format is inspired by https://security.alpinelinux.org/vuln/CVE-2024-5535
// Let's generate a markdown file with the following format!
// H1
var date = cves.Date;

if (DateOnly.TryParse(date, out DateOnly dateOnly))
{
    date = dateOnly.ToShortDateString();
}

writer.WriteLine($""""
# CVE Tracker for {date} .NET Release

The following vulnerabilities were disclosed this month.
""");

// CVE table
string[] cveLabels = ["CVE", "Description", "Product", "Platforms", "CVSS"];
int[] cveLengths = [16, 20, 16, 16, 20];
Table cveTable = new(Writer.GetWriter(writer), cveLengths);

cveTable.WriteHeader(cveLabels);

foreach (Cve cve in cves.Cves)
{cveTable.WriteColumn($"[{cve.Id}][{cve.Id}]");
    cveTable.WriteColumn(cve.Description);
    cveTable.WriteColumn(cve.Product);
    cveTable.WriteColumn(Join(cve.Platforms));
    cveTable.WriteColumn(cve?.Cvss ?? "");
    cveTable.EndRow();
}

writer.WriteLine();

// Package version table
writer.WriteLine("## Vulnerable and patched packages");
writer.WriteLine();
writer.WriteLine("The following table lists vulnerable and patched version ranges for affected packages.");
writer.WriteLine();

string[] packageLabels = ["CVE", "Package", "Min Version", "Max Version", "Fixed Version"];
int[] packageLengths = [16, 16, 12, 12, 16];
Table packageTable = new(Writer.GetWriter(writer), packageLengths);

packageTable.WriteHeader(packageLabels);

foreach (Cve cve in cves.Cves)
{
    foreach (var package in cve.Packages)
    {
        packageTable.WriteColumn($"[{cve.Id}][{cve.Id}]");
        packageTable.WriteColumn(Report.MakePackageString(package.Name));
        packageTable.WriteColumn($">={package.MinVulnerableVersion}");
        packageTable.WriteColumn($"<={package.MaxVulnerableVersion}");
        packageTable.WriteColumn(package.FixedVersion);
        packageTable.EndRow();
    }
}

if (cves?.Commits is null)
{Close(writer, targetFile);
    return;
}


// Commits table
writer.WriteLine();
writer.WriteLine("## Commits");
writer.WriteLine();
writer.WriteLine("The following table lists commits for affected packages.");
writer.WriteLine();

string[] commitLabels = ["CVE", "Branch", "Commit"];
int[] commitLengths = [30, 20, 60];
Table commitTable = new Table(Writer.GetWriter(writer), commitLengths);

commitTable.WriteHeader(commitLabels);

foreach (Commit commit in cves.Commits)
{commitTable.WriteColumn($"[{commit.Cve}][{commit.Cve}]");
    commitTable.WriteColumn(Report.MakeLinkFromBestSource(commit, commit.Branch, cves.Source.BranchUrl, null));
    commitTable.WriteColumn(Report.MakeLinkFromBestSource(commit, null, cves.Source.CommitUrl, commit.Url));
    commitTable.EndRow();
}

writer.WriteLine();

// Write second part of reference-style links
foreach (var cve in cves.Cves)
{writer.WriteLine($"[{cve.Id}]: {Report.MakeCveLink(cve)}");
}

foreach (var cve in cves.Cves)
{
    foreach (var package in cve.Packages)
    {
        if (Report.IsFramework(package.Name))
        {
            continue;
        }

        writer.WriteLine($"[{package.Name}]: {Report.MakeNuGetLink(package.Name)}");
    }
}

// Close file
Close(writer, targetFile);

static void Close(StreamWriter writer, string file)
{writer.Close();
    var writtenFile = File.OpenRead(file);
    long length = writtenFile.Length;
    string path = writtenFile.Name;
    writtenFile.Close();

    Console.WriteLine($"Generated {length} bytes");
    Console.WriteLine(path);
}

static string Join(IEnumerable<string>? strings) => strings is null ? "" : string.Join(", ", strings);

static void ReportInvalidArgs()
{Console.WriteLine("Invalid args.");
    Console.WriteLine("Usage: CveMarkdown path-to-cves.json [target-file]");
}
