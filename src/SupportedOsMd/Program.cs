using System.Reflection;
using System.Text.RegularExpressions;
using DotnetRelease;
using DotnetRelease.Helper;
using EndOfLifeDate;
using MarkdownHelpers;

// Invocation forms:
// SupportedOsMd 8.0
// SupportedOsMd 8.0 ~/git/core/release-notes
// SupportedOsMd 8.0 https://builds.dotnet.microsoft.com/dotnet/release-metadata/

if (args.Length is 0 || !int.TryParse(args[0], out int ver))
{
    ReportInvalidArgs();
    return;
}

const string placeholder = "SECTION-";
const string id = "{ID-";
string templateFile = $"supported-os-template.md";
string targetFile = $"supported-os.md";
string targetDirectory = args.Length > 2 ? args[2] : "";
string targetPath = Path.Combine(targetDirectory, targetFile);
string exeLocation = Assembly.GetExecutingAssembly().Location;
string directory = Path.GetDirectoryName(exeLocation) ?? throw new();
string template = Path.Combine(directory, templateFile);

string version = $"{ver}.0";
string baseUrl = args.Length > 1 ? args[1] : ReleaseNotes.GitHubBaseUri;

// Acquire JSON data, locally or from the web
HttpClient client = new();
AdaptiveLayout layout = new(baseUrl, client);
string supportJson = layout.GetLocation(version, ReleaseNotes.SupportedOS);
using Stream supportStream = await layout.GetStreamAsync(supportJson);
SupportedOSMatrix matrix = await ReleaseNotes.GetSupportedOSes(supportStream) ?? throw new();
string releaseIndexJson = layout.GetLocation(ReleaseNotes.MajorReleasesIndex);
using Stream releaseIndexStream = await layout.GetStreamAsync(releaseIndexJson);
MajorReleasesIndex index = await ReleaseNotes.GetMajorReleasesIndex(releaseIndexStream) ?? throw new();

FileStream stream = File.Open(targetPath, FileMode.Create);
StreamWriter writer = new(stream);
Link pageLinks = new();
Dictionary<string, string> replacements = [];
bool replacementsFound = GetReplacementsForVersion(index, matrix, version, replacements);

foreach (string line in File.ReadLines(template))
{
    if (replacementsFound && line.Contains(id))
    {
        string text = line;
        while (text.Contains(id))
        {
            int start = text.IndexOf(id);
            int end = text.IndexOf('}', start);
            int count = end - start;
            string term = text.Substring(start, count + 1);
            string replacement = replacements[term];
            text = text.Replace(term, replacement);
        }

        writer.WriteLine(text);
        continue;
    }
    else if (!line.StartsWith(placeholder))
    {
        writer.WriteLine(line);
        continue;
    }

    if (line.StartsWith("SECTION-LASTUPDATED"))
    {
        WriteLastUpdatedSection(writer, matrix?.LastUpdated ?? throw new());
    }
    else if (line.StartsWith("SECTION-FAMILIES"))
    {
        WriteFamiliesSection(writer, matrix?.Families ?? throw new(), pageLinks);
    }
    else if (line.StartsWith("SECTION-LIBC") && matrix?.Libc is { })
    {
        WriteLibcSection(writer, matrix.Libc);
    }
    else if (line.StartsWith("SECTION-NOTES") && matrix?.Notes is { })
    {
        WriteNotesSection(writer, matrix.Notes);
    }
    else if (line.StartsWith("SECTION-UNSUPPORTED"))
    {
        await WriteUnSupportedSection(writer, matrix?.Families ?? throw new(), client);
    }
}

if (pageLinks.Count > 0)
{
    writer.WriteLine();

    foreach (string link in pageLinks.GetReferenceLinkAnchors())
    {
        writer.WriteLine(link);
    }
}

writer.Close();
var writtenFile = File.OpenRead(targetPath);
long length = writtenFile.Length;
string path = writtenFile.Name;
writtenFile.Close();

Console.WriteLine($"Generated {length} bytes");
Console.WriteLine(path);

static void ReportInvalidArgs()
{
    Console.WriteLine("Invalid args.");
    Console.WriteLine("Expected: version [URL or Path, absolute or root location]");
}

static void WriteLastUpdatedSection(StreamWriter writer, DateOnly date)
{
    writer.WriteLine($"Last updated: {date.ToString("yyyy-MM-dd")}");
}

static void WriteFamiliesSection(StreamWriter writer, IList<SupportFamily> families, Link links)
{
    string[] labels = ["OS", "Versions", "Architectures", "Lifecycle"];
    int[] lengths = [32, 30, 24, 24];
    int linkCount = 0;

    foreach (SupportFamily family in families)
    {
        Table table = new(Writer.GetWriter(writer), lengths);
        Link familyLinks = new(linkCount);
        writer.WriteLine($"## {family.Name}");
        writer.WriteLine();
        table.WriteHeader(labels);
        List<string> notes = [];

        for (int i = 0; i < family.Distributions.Count; i++)
        {
            SupportDistribution distro = family.Distributions[i];
            IList<string> distroVersions = distro.SupportedVersions;

            if (distro.Name is "Windows")
            {
                distroVersions = SupportedOS.SimplifyWindowsVersions(distro.SupportedVersions);
            }

            string versions = "";
            if (distroVersions.Count is 0)
            {
                var noneLink = links.AddReferenceLink("None", "#out-of-support-os-versions");
                versions = noneLink;
            }
            else
            {
                versions = Join(distroVersions);
            }

            var distroLink = familyLinks.AddIndexReferenceLink(distro.Name, distro.Link);
            table.WriteColumn(distroLink);
            table.WriteColumn(versions);
            table.WriteColumn(Join(distro.Architectures));
            var lifecycleLink = distro.Lifecycle is null ? "None" :
                familyLinks.AddIndexReferenceLink("Lifecycle", distro.Lifecycle);

            table.WriteColumn(lifecycleLink);
            table.EndRow();
            linkCount = familyLinks.Index;

            if (distro.Notes is { Count: > 0 })
            {
                foreach (string note in distro.Notes)
                {
                    notes.Add($"{distro.Name}: {note}");
                }
            }
        }

        if (notes.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("Notes:");
            writer.WriteLine();

            foreach (string note in notes)
            {
                writer.WriteLine($"* {note}");
            }
        }

        writer.WriteLine();

        foreach (var refLink in familyLinks.GetReferenceLinkAnchors())
        {
            writer.WriteLine(refLink);
        }

        writer.WriteLine();
    }
}

static void WriteLibcSection(StreamWriter writer, IList<SupportLibc> supportedLibc)
{
    string[] columnLabels = ["Libc", "Version", "Architectures", "Source"];
    int[] columnLengths = [16, 10, 24, 16];
    Table table = new(Writer.GetWriter(writer), columnLengths);
    table.WriteHeader(columnLabels);

    foreach (SupportLibc libc in supportedLibc)
    {
        table.WriteColumn(libc.Name);
        table.WriteColumn(libc.Version);
        table.WriteColumn(Join(libc.Architectures));
        table.WriteColumn(libc.Source);
        table.EndRow();
    }
}

static void WriteNotesSection(StreamWriter writer, IList<string> notes)
{
    foreach (string note in notes)
    {
        writer.WriteLine($"* {note}");
    }
}

static async Task WriteUnSupportedSection(StreamWriter writer, IList<SupportFamily> families, HttpClient client)
{
    // Get all unsupported cycles in parallel.
    var eolCycles = await Task.WhenAll(families
        .SelectMany(f => f.Distributions
            .SelectMany(d => (d.UnsupportedVersions ?? [])
                .Select(async v => new
                {
                    Distribution = d,
                    Version = v,
                    Cycle = await GetProductCycle(client, d, v)
                }))));

    // Order the list of cycles by their EoL date.
    var orderedEolCycles = eolCycles
        .OrderBy(entry => entry.Distribution.Name)
        .ThenByDescending(entry => GetEolDateForCycle(entry.Cycle))
        .ToArray();

    if (eolCycles.Length == 0)
    {
        writer.WriteLine("None currently.");
        return;
    }

    string[] labels = ["OS", "Version", "Date"];
    int[] lengths = [24, 16, 24];
    Table table = new(Writer.GetWriter(writer), lengths);

    table.WriteHeader(labels);

    foreach (var entry in orderedEolCycles)
    {
        var eol = GetEolTextForCycle(entry.Cycle);
        var distroName = entry.Distribution.Name;
        var distroVersion = entry.Version;
        if (distroName is "Windows")
        {
            distroVersion = SupportedOS.PrettyifyWindowsVersion(distroVersion);
        }

        table.WriteColumn(distroName);
        table.WriteColumn(distroVersion);
        table.WriteColumn(eol);
        table.EndRow();
    }
}

static string GetEolTextForCycle(SupportCycle? cycle)
{
    var eolDate = GetEolDateForCycle(cycle);
    if (cycle == null || eolDate == DateOnly.MinValue) return "-";

    var result = eolDate.ToString("yyyy-MM-dd");
    var link = cycle.Link;
    if (link != null)
    {
        result = $"[{result}]({link})";
    }
    return result;
}

static async Task<SupportCycle?> GetProductCycle(HttpClient client, SupportDistribution distro, string unsupportedVersion)
{
    try
    {
        return await EndOfLifeDate.Product.GetProductCycle(client, distro.Id, unsupportedVersion);
    }
    catch (HttpRequestException)
    {
        Console.WriteLine($"No data found at endoflife.date for: {distro.Id} {unsupportedVersion}");
        Console.WriteLine();
        return null;
    }
}

static DateOnly GetEolDateForCycle(SupportCycle? supportCycle)
{
    return supportCycle?.GetSupportInfo().EolDate ?? DateOnly.MinValue;
}

static string Join(IEnumerable<string>? strings) => strings is null ? "" : string.Join(", ", strings);

// Replacements:
// ID-VERSION
// ID-LASTUPDATED
// ID-SUPPORT-PHASE
// ID-RELEASE-TYPE
static bool GetReplacementsForVersion(MajorReleasesIndex index, SupportedOSMatrix matrix, string version, Dictionary<string, string> replacements)
{
    MajorReleaseIndexItem? release = index.ReleasesIndex.Where(r => r.ChannelVersion == version).FirstOrDefault();

    if (release is null)
    {
        return false;
    }

    replacements.Add("{ID-VERSION}", release.ChannelVersion);
    replacements.Add("{ID-SUPPORT-PHASE}", release.SupportPhase.ToString());
    replacements.Add("{ID-RELEASE-TYPE}", release.ReleaseType.ToString());
    replacements.Add("{ID-LASTUPDATED}", matrix.LastUpdated.ToString("yyyy/MM/dd"));

    return true;
}