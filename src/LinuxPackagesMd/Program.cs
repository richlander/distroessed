﻿using System.Text;
using DotnetRelease;
using MarkdownHelpers;

// if (args.Length is 0 || !int.TryParse(args[0], out int ver))
// {
//     ReportInvalidArgs();
//     return;
// }

int ver = 9;

string version = $"{ver}.0";
string baseDefaultURL = "https://raw.githubusercontent.com/dotnet/core/linux-packages/release-notes/";
string baseUrl = args.Length > 1 ? args[1] : baseDefaultURL;
bool preferWeb = baseUrl.StartsWith("https");
string packageJson = baseUrl;

if (!packageJson.EndsWith(".json"))
{
    packageJson = ReleaseNotes.GetUri(ReleaseNotes.OSPackages, version, baseUrl);
}

string file = "os-packages.md";
HttpClient client = new();
FileStream stream = File.Open(file, FileMode.Create);
StreamWriter writer = new(stream);
OSPackagesOverview? packageOverview = null;

if (preferWeb)
{
    packageOverview = await ReleaseNotes.GetOSPackages(client, packageJson) ?? throw new();
}
else
{
    packageOverview = await ReleaseNotes.GetOSPackages(File.OpenRead(packageJson)) ?? throw new();
}

writer.WriteLine("# .NET 9 Required Packages");
writer.WriteLine();
writer.WriteLine("Various packages must be installed to run .NET apps and the .NET SDK. This is handled automatically if .NET is [installed through archive packages](../../linux.md).");
writer.WriteLine();
writer.WriteLine("This file is generated from [os-packages.json](os-packages.json).");
writer.WriteLine();

writer.WriteLine("## Package Overview");
writer.WriteLine();
writer.WriteLine("The following table lists required packages, including the scenarios by which they are needed.");
writer.WriteLine();

string[] packageLabels = ["Id", "Name", "Required scenarios", "Notes"];
int[] packageColumns = [16, 12, 16, 32];
Table packageTable = new(Writer.GetWriter(writer), packageColumns);
Link link = new();
packageTable.WriteHeader(packageLabels);

foreach (var package in packageOverview.Packages)
{
    BreakBuffer buffer = new(new());
    if (package.MinVersion is {})
    {
        buffer.Append($"Minimum required version {package.MinVersion}");
    }

    buffer.AppendRange(package.References ?? []);

    var pkgLink = link.AddIndexReferenceLink(package.Id, $"https://pkgs.org/search/?q={package.Id}");
    packageTable.WriteColumn(pkgLink);
    packageTable.WriteColumn(package.Name);
    packageTable.WriteColumn(string.Join("<br>", package.RequiredScenarios ?? []));
    packageTable.WriteColumn(buffer.ToString());
    packageTable.EndRow();
}

writer.WriteLine();

foreach (var refLink in link.GetReferenceLinkAnchors())
{
    writer.WriteLine(refLink);
}

foreach (var distro in packageOverview.Distributions)
{
    writer.WriteLine();
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

writer.Close();

// static void ReportInvalidArgs()
// {
//     Console.WriteLine("Invalid args.");
//     Console.WriteLine("Expected: version [URL or Path, absolute or root location]");
// }


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
