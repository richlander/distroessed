using DotnetSupport;
using EndOfLifeDate;
using MarkdownHelpers;

if (args.Length is 0 || !int.TryParse(args[0], out int ver))
{
    ReportInvalidArgs();
    return;
}

const string oosLink = "[OOS]: #out-of-support-os-versions";
bool hasANoSupportedOS = false;
string version = $"{ver}.0";
string baseDefaultURL = "https://raw.githubusercontent.com/dotnet/core/main/release-notes/";
string baseUrl = args.Length > 1 ? args[1] : baseDefaultURL;
bool preferWeb = baseUrl.StartsWith("https");
string supportJson = baseUrl;

if (!supportJson.EndsWith(".json"))
{
    supportJson = preferWeb ?
        $"{baseUrl}/{version}/supported-os.json" :
        Path.Combine(baseUrl, version,"supported-os.json");
}

string template = $"supported-os-template{ver}.md";
string file = $"supported-os{ver}.md";
string placeholder = "PLACEHOLDER-";
HttpClient client = new();
FileStream stream = File.Open(file, FileMode.Create);
StreamWriter writer = new(stream);

SupportMatrix? matrix = null;

if (preferWeb)
{
    matrix = await SupportedOS.GetSupportMatrix(client, supportJson) ?? throw new();
}
else
{
    matrix = await SupportedOS.GetSupportMatrix(File.OpenRead(supportJson)) ?? throw new();
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
        WriteFamiliesSection(writer, matrix.Families, out hasANoSupportedOS);
    }
    else if (line.StartsWith("PLACEHOLDER-LIBC"))
    {
        WriteLibcSection(writer, matrix.Libc);
    }
    else if (line.StartsWith("PLACEHOLDER-NOTES"))
    {
        WriteNotesSection(writer, matrix.Notes);
    }
    else if (line.StartsWith("PLACEHOLDER-UNSUPPORTED"))
    {
        await WriteUnSupportedSection(writer, matrix.Families);
    }
}

if (hasANoSupportedOS)
{
    writer.WriteLine();
    writer.WriteLine(oosLink);
}

writer.Close();
var writtenFile = File.OpenRead(file);
long length = writtenFile.Length;
string path = writtenFile.Name;
writtenFile.Close();

Console.WriteLine($"Generated {length} bytes");
Console.WriteLine(path);

void ReportInvalidArgs()
{
    Console.WriteLine("Invalid args.");
    Console.WriteLine("Expected: version [URL or Path, absolute or root location]");
}

void WriteLastUpdatedSection(StreamWriter writer, DateOnly date)
{
    writer.WriteLine($"Last updated: {date.ToString("yyyy-MM-dd")}");
}

void WriteFamiliesSection(StreamWriter writer, IList<SupportFamily> families, out bool hasANoSupportedOS)
{
    hasANoSupportedOS = false;
    string[] labels = [ "OS", "Versions", "Architectures", "Lifecycle" ];
    int[] lengths = [32, 30, 20, 20];
    int linkCount = 0;

    foreach (SupportFamily family in families)
    {
        writer.WriteLine($"## {family.Name}");
        writer.WriteLine();

        if (family.Name is "Apple")
        {
            Markdown.WriteHeader(writer, labels, [32, 30, 20]);
        }
        else
        {
            Markdown.WriteHeader(writer, labels, lengths);
        }

        int link = linkCount;
        List<string> notes = [];

        for (int i = 0; i < family.Distributions.Count; i++)
        {
            SupportDistribution distro = family.Distributions[i];
            IList<string> distroVersions = distro.SupportedVersions;

            if (distro.Name is "Windows")
            {
                distroVersions = SupportedOS.SimplifyWindowsVersions(distro.SupportedVersions);
            }

            int column = 0;
            string versions = "";
            if (distroVersions.Count is 0)
            {
                versions = "[None][OOS]";
                hasANoSupportedOS = true;
            }
            else
            {
                versions = Join(distroVersions);
            }

            Markdown.WriteColumn(writer, $"[{distro.Name}][{link++}]", lengths[column++], false);
            Markdown.WriteColumn(writer, versions, lengths[column++]);
            Markdown.WriteColumn(writer, Join(distro.Architectures), lengths[column++]);
            if (distro.Lifecycle is not null)
            {
                Markdown.WriteColumn(writer, $"[Lifecycle][{link++}]", lengths[column++]);
            }

            writer.WriteLine();

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

        foreach (SupportDistribution distro in family.Distributions)
        {
            writer.WriteLine($"[{linkCount++}]: {distro.Link}");
            if (distro.Lifecycle is {})
            {
                writer.WriteLine($"[{linkCount++}]: {distro.Lifecycle}");
            }
        }

        writer.WriteLine();
    }
}

void WriteLibcSection(StreamWriter writer, IList<SupportLibc> supportedLibc)
{
    string[] columnLabels = [ "Libc", "Version", "Architectures", "Source"];
    int[] columnLengths = [25, 10, 20, 20 ];
    Markdown.WriteHeader(writer, columnLabels, columnLengths);

    foreach (SupportLibc libc in supportedLibc)
    {
        int column = 0;
        Markdown.WriteColumn(writer, libc.Name, columnLengths[column++], false);
        Markdown.WriteColumn(writer, libc.Version, columnLengths[column++]);
        Markdown.WriteColumn(writer, Join(libc.Architectures), columnLengths[column++]);
        Markdown.WriteColumn(writer, libc.Source, columnLengths[column++]);
        writer.WriteLine();
    }
}

void WriteNotesSection(StreamWriter writer, IList<string> notes)
{
    foreach (string note in notes)
    {
        writer.WriteLine($"* {note}");
    }
}

async Task WriteUnSupportedSection(StreamWriter writer, IList<SupportFamily> families)
{
    // Get all unsupported cycles in parallel.
    var eolCycles = await Task.WhenAll(families
        .SelectMany(f => f.Distributions
            .SelectMany(d => (d.UnsupportedVersions ?? [])
                .Select(async v => new
                {
                    Distribution = d,
                    Version = v,
                    Cycle = await GetProductCycle(d, v)
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
    int[] lengths = [32, 30, 20];
    
    Markdown.WriteHeader(writer, labels, lengths);

    foreach (var entry in orderedEolCycles)
    {
        var eol = GetEolTextForCycle(entry.Cycle);
        var distroName = entry.Distribution.Name;
        var distroVersion = entry.Version;
        if (distroName is "Windows")
        {
            distroVersion = SupportedOS.PrettyifyWindowsVersion(distroVersion);
        }

        int column = 0;
        Markdown.WriteColumn(writer, distroName, lengths[column++], false);
        Markdown.WriteColumn(writer, distroVersion, lengths[column++]);
        Markdown.WriteColumn(writer, eol, lengths[column++]);
        writer.WriteLine();
    }
}

string GetEolTextForCycle(SupportCycle? cycle)
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

async Task<SupportCycle?> GetProductCycle(SupportDistribution distro, string unsupportedVersion)
{
    try
    {
        return await Product.GetProductCycle(client, distro.Id, unsupportedVersion);
    }
    catch (HttpRequestException)
    {
        Console.WriteLine($"No data found at endoflife.date for: {distro.Id} {unsupportedVersion}");
        Console.WriteLine();
        return null;
    }
}

DateOnly GetEolDateForCycle(SupportCycle? supportCycle)
{
    return supportCycle?.GetSupportInfo().EolDate ?? DateOnly.MinValue;
}

static string Join(IEnumerable<string>? strings) => strings is null ? "" : string.Join(", ", strings);
