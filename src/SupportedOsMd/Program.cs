using DotnetRelease;
using EndOfLifeDate;
using MarkdownHelpers;

if (args.Length is 0 || !int.TryParse(args[0], out int ver))
{
    ReportInvalidArgs();
    return;
}

string version = $"{ver}.0";
string? baseUrl = args.Length > 1 ? args[1] : null;
string supportJson = baseUrl ?? string.Empty;

if (!supportJson.EndsWith(".json"))
{
    supportJson = ReleaseNotes.GetUri(ReleaseNotes.SupportedOS, version, baseUrl);
}

string template = $"supported-os-template{ver}.md";
string file = $"supported-os{ver}.md";
string placeholder = "PLACEHOLDER-";
HttpClient client = new();
FileStream stream = File.Open(file, FileMode.Create);
StreamWriter writer = new(stream);
SupportedOSMatrix? matrix = null;
Link pageLinks = new();
bool preferWeb = supportJson.StartsWith("https");

if (preferWeb)
{
    matrix = await ReleaseNotes.GetSupportedOSes(client, supportJson) ?? throw new();
}
else
{
    matrix = await ReleaseNotes.GetSupportedOSes(File.OpenRead(supportJson)) ?? throw new();
}

foreach (string line in File.ReadLines(template))
{
    if (!line.StartsWith(placeholder))
    {
        writer.WriteLine(line);
        continue;
    }

    if (line.StartsWith("PLACEHOLDER-LASTUPDATED"))
    {
        WriteLastUpdatedSection(writer, matrix.LastUpdated);
    }
    else if (line.StartsWith("PLACEHOLDER-FAMILIES"))
    {
        WriteFamiliesSection(writer, matrix.Families, pageLinks);
    }
    else if (line.StartsWith("PLACEHOLDER-LIBC") && matrix.Libc is {})
    {
        WriteLibcSection(writer, matrix.Libc);
    }
    else if (line.StartsWith("PLACEHOLDER-NOTES") && matrix.Notes is {})
    {
        WriteNotesSection(writer, matrix.Notes);
    }
    else if (line.StartsWith("PLACEHOLDER-UNSUPPORTED"))
    {
        await WriteUnSupportedSection(writer, matrix.Families, client);
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
var writtenFile = File.OpenRead(file);
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
    string[] labels = [ "OS", "Versions", "Architectures", "Lifecycle" ];
    int[] lengths = [32, 30, 24, 24];
    Table defaultTable = new(Writer.GetWriter(writer), lengths);
    int linkCount = 0;

    foreach (SupportFamily family in families)
    {
        Table table = defaultTable;
        Link familyLinks = new(linkCount);
        writer.WriteLine($"## {family.Name}");
        writer.WriteLine();

        if (family.Name is "Apple")
        {
            int[] appleLengths = [32, 30, 24];
            table = new(Writer.GetWriter(writer), appleLengths);
            table.WriteHeader(labels.AsSpan(0, 3));
        }
        else
        {
            table.WriteHeader(labels);
        }

        List<string> notes = [];

        for (int i = 0; i < family.Distributions.Count; i++)
        {
            SupportDistribution distro = family.Distributions[i];
            IList<string> distroVersions = distro.SupportedVersions;
            bool hasLifecycle = distro.Lifecycle is not null;

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

            if (distro.Lifecycle is not null)
            {
                var lifecycleLink = familyLinks.AddIndexReferenceLink("Lifecycle", distro.Lifecycle);
                table.WriteColumn(lifecycleLink);
            }

            table.EndRow();
            linkCount = familyLinks.Index;

            if (distro.Notes is {Count: > 0})
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
    string[] columnLabels = [ "Libc", "Version", "Architectures", "Source"];
    int[] columnLengths = [16, 10, 24, 16 ];
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
    
    string[] labels = [ "OS", "Version", "Date" ];
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
