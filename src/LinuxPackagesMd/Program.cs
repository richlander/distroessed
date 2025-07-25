using System.Text;
using DotnetRelease;
using FileHelpers;
using MarkdownHelpers;

if (args.Length is 0 || !int.TryParse(args[0], out int majorVersion))
{
    ReportInvalidArgs();
    return;
}

// Version strings
string version = $"{majorVersion}.0";

// Get path adaptor
string basePath = args.Length > 1 ? args[1] : ReleaseNotes.GitHubBaseUri;
using HttpClient client = new();
IAdaptivePath path = AdaptivePath.GetFromDefaultAdaptors(basePath, client);

// Paths
string packageJson = path.Combine(version, ReleaseNotes.OSPackages);
string packageTemplate = path.Combine("templates", "os-packages-template.md");
string targetFile = ReleaseNotes.OSPackages.Replace(".json", ".md");
string targetPath = path.SupportsLocalPaths ? path.Combine(version, targetFile) : targetFile;

// Acquire JSON data, locally or from the web
using Stream packageStream = await path.GetStreamAsync(packageJson);
OSPackagesOverview packageOverview = await ReleaseNotes.GetOSPackages(packageStream) ?? throw new();

// Open streams
using FileStream targetStream = File.Open(targetPath, FileMode.Create);
using StreamWriter targetWriter = new(targetStream);
using Stream templateStream = await path.GetStreamAsync(packageTemplate);
using StreamReader templateReader = new(templateStream);

// Replacement strings
Dictionary<string, string> replacements = [];
replacements.Add("VERSION", version);

Link pageLinks = new();

MarkdownTemplate notes = new()
{
    Processor = (id, writer) =>
    {
        Console.WriteLine($"Processing token: {id}");
        if (replacements.TryGetValue(id, out string? value))
        {
            writer.Write(value);
            return;
        }

        switch (id)
        {
            case "OVERVIEW":
                WritePackageOverview(writer, packageOverview, pageLinks);
                break;
            case "FAMILIES":
                WritePackageFamilies(writer, packageOverview);
                break;
            default:
                throw new($"Unknown token: {id}");
        }
    }
};
notes.Process(templateReader, targetWriter);

templateReader.Close();
templateStream.Close();
targetWriter.Close();
targetStream.Close();
var writtenFile = File.OpenRead(targetPath);
Console.WriteLine($"Generated {writtenFile.Length} bytes");
Console.WriteLine(writtenFile.Name);
writtenFile.Close();

static void WritePackageOverview(StreamWriter writer, OSPackagesOverview packageOverview, Link links)
{
    ReadOnlySpan<string> packageLabels = ["Id", "Name", "Required scenarios", "Notes"];
    Table packageTable = new();
  
    packageTable.WriteHeader(packageLabels);

    foreach (var package in packageOverview.Packages)
    {
        BreakBuffer buffer = new(new());
        if (package.MinVersion is {})
        {
            buffer.Append($"Minimum required version {package.MinVersion}");
        }

        buffer.LinkFormat = true;
        buffer.AppendRange(package.References ?? []);
        buffer.LinkFormat = false;


        var pkgLink = links.AddIndexReferenceLink(package.Id, $"https://pkgs.org/search/?q={package.Id}");
        packageTable.NewRow();
        packageTable.WriteColumn(pkgLink);
        packageTable.WriteColumn(package.Name);
        packageTable.WriteColumn(string.Join(" ; ", package.RequiredScenarios ?? []));
        packageTable.WriteColumn(buffer.ToString());
    }
    
    writer.Write(packageTable);

    foreach (var refLink in links.GetReferenceLinkAnchors())
    {
        writer.WriteLine(refLink);
    }
}

static void WritePackageFamilies(StreamWriter writer, OSPackagesOverview packageOverview)
{
    bool first = true;

    foreach (var distro in packageOverview.Distributions)
    {
        if (first)
        {
            first = false;
        }
        else
        {
            writer.WriteLine();
        }

        writer.WriteLine($"## {distro.Name}");

        Guard guard = new(Writer.GetWriter(writer));

        foreach (var release in distro.Releases)
        {
            writer.WriteLine();
            writer.WriteLine($"### {release.Name}");
            writer.WriteLine();

            guard.StartRegion("bash");

            int commandCount = distro?.InstallCommands?.Count is null ? 0 : distro.InstallCommands.Count;
            for(int i = 0; i < commandCount; i++)
            {
                var command = distro!.InstallCommands![i];
                var commandString = GetCommandString(command);
                guard.Write(commandString);

                if (i + 1 < commandCount)
                {
                    guard.Write(" &&");
                }

                guard.WriteLine($" \\");
            }

            guard.UpdateIndent(4);

            int count = release.Packages.Count;
            var packages = release.Packages.OrderBy(p => p.Name).ToList();
            for (int i = 0; i < count; i++)
            {
                string endChar = "";
                if (i + 1 < count)
                {
                    endChar = " \\";
                }

                guard.WriteLine($"{packages[i].Name}{endChar}");
            }

            guard.EndRegion();  
        }
    }
}

static string GetCommandString(Command command)
{
    StringBuilder builder = new();

    if (command.RunUnderSudo)
    {
        builder.Append("sudo ");
    }

    builder.Append(command.CommandRoot);

    foreach (var part in command.CommandParts ?? [])
    {
        if (part == "{packageName}")
        {
            continue;
        }

        builder.Append($" {part}");
    }

    return builder.ToString();
}

static void ReportInvalidArgs()
{
    Console.WriteLine("Invalid args.");
    Console.WriteLine("Expected: version [URL or Path, absolute or root location]");
}
