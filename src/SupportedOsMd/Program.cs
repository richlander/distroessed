using System.Reflection;
using System.Text.RegularExpressions;
using DotnetRelease;
using FileHelpers;
using EndOfLifeDate;
using MarkdownHelpers;
using System.Collections;
using System.ComponentModel;

// Invocation forms:
// SupportedOsMd 8
// SupportedOsMd 8 ~/git/core/release-notes
// SupportedOsMd 8 https://builds.dotnet.microsoft.com/dotnet/release-metadata/

// Static strings
const string templates = "templates";
const string templateFile = $"supported-os-template.md";
const string targetFile = $"supported-os.md";

if (args.Length is 0 || !int.TryParse(args[0], out int majorVersion))
{
    ReportInvalidArgs();
    return;
}

// Version strings
string version = $"{majorVersion}.0";

// Get path adaptor
string basePath = args.Length > 1 ? args[1] : Location.GitHubBaseUri;
using HttpClient client = new();
IAdaptivePath path = AdaptivePath.GetFromDefaultAdaptors(basePath, client);

// Paths
string templatePath = path.Combine(templates, templateFile);
string targetPath = path.SupportsLocalPaths ? path.Combine(version, targetFile) : targetFile;

// Acquire JSON data, locally or from the web
string supportJson = path.Combine(version, ReleaseNotes.SupportedOS);
using Stream supportStream = await path.GetStreamAsync(supportJson);
SupportedOSMatrix matrix = await ReleaseNotes.GetSupportedOSes(supportStream) ?? throw new();
string releaseIndexJson = path.Combine(ReleaseNotes.MajorReleasesIndex);
using Stream releaseIndexStream = await path.GetStreamAsync(releaseIndexJson);
MajorReleasesIndex index = await ReleaseNotes.GetMajorReleasesIndex(releaseIndexStream) ?? throw new();

DateTime now = DateTime.Now;
DateOnly today = DateOnly.FromDateTime(now);

if (matrix.LastUpdated < today)
{
    Console.WriteLine($"Warning: Last updated date {matrix.LastUpdated} is older than today.");
}

bool releaseFound = index.ReleasesIndex.Any(r => r.ChannelVersion == version);

// Open template
using Stream templateStream = await path.GetStreamAsync(templatePath);
using StreamReader templateReader = new(templateStream);

// Open target file
using FileStream targetStream = File.Open(targetPath, FileMode.Create);
using StreamWriter targetWriter = new(targetStream);

// Process template and write output
Link pageLinks = new();
Dictionary<string, string> replacements = [];
replacements.Add("LASTUPDATED", matrix.LastUpdated.ToString("yyyy/MM/dd"));
replacements.Add("VERSION", version);
if (releaseFound)
{
    MajorReleaseIndexItem release = index.ReleasesIndex.Where(r => r.ChannelVersion == version).FirstOrDefault() ?? 
        throw new Exception($"No release found for version {version}");

    replacements.Add("SUPPORT-PHASE", release.SupportPhase.ToString());
    replacements.Add("RELEASE-TYPE", release.ReleaseType.ToString());
}
else // should only be relevant for pre-Preview 1 timeframe
{
    replacements.Add("SUPPORT-PHASE", "Preview");
    replacements.Add("RELEASE-TYPE", "Unknown");
}

MarkdownTemplate notes = new()
{
    Processor = (id, writer) =>
    {
        if (replacements.TryGetValue(id, out string? value))
        {
            writer.Write(value);
            return;
        }
    },
    AsyncProcessor = async (id, writer) =>
    {
        switch (id)
        {
            case "LASTUPDATED":
                WriteLastUpdatedSection(writer, matrix?.LastUpdated ?? throw new());
                break;
            case "FAMILIES":
                WriteFamiliesSection(writer, matrix?.Families ?? throw new(), pageLinks);
                break;
            case "LIBC":
                WriteLibcSection(writer, matrix?.Libc ?? throw new("Libc section not found"));
                break;
            case "NOTES":
                WriteNotesSection(writer, matrix?.Notes ?? throw new("Notes section not found"));
                break;
            case "UNSUPPORTED":
                await WriteUnSupportedSection(writer, matrix?.Families ?? throw new("EOL data not found"), client);
                break;
            default:
                throw new($"Unknown token: {id}");
        }
    }
};

await notes.ProcessAsync(templateReader, targetWriter);

if (pageLinks.Count > 0)
{
    targetWriter.WriteLine();

    foreach (string link in pageLinks.GetReferenceLinkAnchors())
    {
        targetWriter.WriteLine(link);
    }
}

templateReader.Close();
templateStream.Close();
targetWriter.Close();
targetStream.Close();
var writtenFile = File.OpenRead(targetPath);
Console.WriteLine($"Generated {writtenFile.Length} bytes");
Console.WriteLine(writtenFile.Name);
writtenFile.Close();

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
    int linkCount = 0;
    bool first = true;

    foreach (SupportFamily family in families)
    {
        if (first)
        {
            first = false;
        }
        else
        {
            writer.WriteLine();
        }

        Table table = new();
        Link familyLinks = new(linkCount);
        writer.WriteLine($"## {family.Name}");
        writer.WriteLine();
        table.AddHeader("OS", "Versions", "Architectures", "Lifecycle");
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
            var lifecycleLink = distro.Lifecycle is null ? "None" :
                familyLinks.AddIndexReferenceLink("Lifecycle", distro.Lifecycle);

            table.AddRow(distroLink, versions, Join(distro.Architectures), lifecycleLink);
            linkCount = familyLinks.Index;

            if (distro.Notes is { Count: > 0 })
            {
                foreach (string note in distro.Notes)
                {
                    notes.Add($"{distro.Name}: {note}");
                }
            }
        }
        
        writer.Write(table);

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
    }
}

static void WriteLibcSection(StreamWriter writer, IList<SupportLibc> supportedLibc)
{
    string[] columnLabels = ["Libc", "Version", "Architectures", "Source"];
    Table table = new();
    table.AddHeader(columnLabels);

    foreach (SupportLibc libc in supportedLibc)
    {
        table.AddRow(libc.Name, libc.Version, Join(libc.Architectures), libc.Source);
    }
    
    writer.Write(table);
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
    Console.WriteLine("Getting EoL data...");
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

    if (orderedEolCycles.Length == 0)
    {
        writer.WriteLine("None currently.");
        return;
    }

    string[] labels = ["OS", "Version", "Date"];
    Table table = new();

    table.AddHeader(labels);

    foreach (var entry in orderedEolCycles)
    {
        var eol = GetEolTextForCycle(entry.Cycle);
        var distroName = entry.Distribution.Name;
        var distroVersion = entry.Version;
        if (distroName is "Windows")
        {
            distroVersion = SupportedOS.PrettyifyWindowsVersion(distroVersion);
        }

        table.AddRow(distroName, distroVersion, eol);
    }
    
    writer.Write(table);
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
