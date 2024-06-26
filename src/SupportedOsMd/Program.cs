using System.Text;
using DotnetSupport;
using SupportedOsMd;

if (args.Length is 0 || !int.TryParse(args[0], out int ver))
{
    ReportInvalidArgs();
    return;
}

string version = $"{ver}.0";
string baseDefaultURL = "https://raw.githubusercontent.com/dotnet/core/os-support/release-notes/";
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
FileStream stream = File.OpenWrite(file);
StreamWriter writer = new(stream);
List<string> unsupportedVersions = [];

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

    if (line.StartsWith("PLACEHOLDER-FAMILIES"))
    {
        WriteFamiliesSection(writer, matrix.Families, unsupportedVersions);
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
        WriteUnSupportedSection(writer, unsupportedVersions);
    }
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

void WriteFamiliesSection(StreamWriter writer, IList<SupportFamily> families, List<string> unsupportedVersions)
{
    string[] columnLabels = [ "OS", "Version", "Architectures", "Lifecycle" ];
    int[] columnLengths = [32, 30, 20, 20 ];
    Columns columns = new(columnLabels, columnLengths);
    int linkCount = 0;

    foreach (SupportFamily family in families)
    {
        writer.WriteLine($"## {family.Name}");
        writer.WriteLine();
        WriteHeader(writer, columns);
        int link = linkCount;
        List<string> notes = [];

        for (int i = 0; i < family.Distributions.Count; i++)
        {
            SupportDistribution distro = family.Distributions[i];
            IList<string> distroVersions = distro.SupportedVersions;
            IList<string> distroUnsupportedVersions = distro.UnsupportedVersions ?? [];

            if (distro.Name is "Windows")
            {
                distroVersions = SupportedOS.SimplifyWindowsVersions(distro.SupportedVersions);
            }

            int column = 0;
            WriteColumn(writer, columnLengths[column++], $"[{distro.Name}][{link++}]", false);
            WriteColumn(writer, columnLengths[column++], MakeString(distroVersions), true);
            WriteColumn(writer, columnLengths[column++], MakeString(distro.Architectures), true);
            WriteColumn(writer, columnLengths[column++], $"[Lifecycle][{link++}]", true);
            writer.WriteLine();

            if (distro.Notes is {Count: > 0})
            {
                foreach (string note in distro.Notes)
                {
                    notes.Add($"{distro.Name}: {note}");
                }
            }

            foreach (string version in distroUnsupportedVersions)
            {
                unsupportedVersions.Add($"{distro.Name} {version}");
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
            writer.WriteLine($"[{linkCount++}]: {distro.Lifecycle}");
        }

        writer.WriteLine();
    }
}

void WriteLibcSection(StreamWriter writer, IList<SupportLibc> supportedLibc)
{
    string[] columnLabels = [ "Libc", "Version", "Architectures", "Source"];
    int[] columnLengths = [25, 10, 20, 20 ];
    Columns columns = new(columnLabels, columnLengths);

    WriteHeader(writer, columns);

    foreach (SupportLibc libc in supportedLibc)
    {
        int column = 0;
        WriteColumn(writer, columnLengths[column++], libc.Name, false);
        WriteColumn(writer, columnLengths[column++], libc.Version, true);
        WriteColumn(writer, columnLengths[column++], MakeString(libc.Architectures), true);
        WriteColumn(writer, columnLengths[column++], libc.Source, true);
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

void WriteUnSupportedSection(StreamWriter writer, IList<string> unsupported)
{
    if (unsupported.Count is 0)
    {
        writer.WriteLine("None currently.");
        return;
    }

    foreach (string version in unsupported)
    {
        writer.WriteLine($"* {version}");
    }
}

void WriteRepeatCharacter(StreamWriter writer, char repeatCharacter, int repeats)
{
    for (int i = 0; i < repeats; i++)
    {
        writer.Write(repeatCharacter);
    }
}

void WriteHeader(StreamWriter writer, Columns columns)
{
    var (columnLabels, columnLengths) = columns;

    for (int i = 0; i < columnLengths.Length; i++)
    {
        WriteColumn(writer, columnLengths[i], columnLabels[i], i > 0);
    }

    writer.WriteLine();

    for (int i = 0; i < columnLengths.Length; i++)
    {
        WriteRepeatCharacter(writer, '-', columnLengths[i]);
        writer.Write("|");
    }

    writer.WriteLine();
}

void WriteColumn(StreamWriter writer, int columnLength, string value, bool frontPad, char repeatCharacter = ' ')
{
    if (frontPad)
    {
        writer.Write(' ');
    }

    writer.Write(value);
    if (value.Length < columnLength)
    {
        int length = frontPad ? columnLength - value.Length - 1 : columnLength - value.Length;
        WriteRepeatCharacter(writer, repeatCharacter, length);
        writer.Write("|");
    }
    else
    {
        writer.Write(" |");
    }
}

string MakeString(IList<string> values)
{
    StringBuilder builder = new();
    for (int i = 0; i < values.Count; i++)
    {
        if (i > 0)
        {
            builder.Append(", ");
        }
        builder.Append(values[i]);
    }

    return builder.ToString();
}

namespace SupportedOsMd
{
    record struct Columns(string[] Labels, int[] Lengths);
}
