using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CveInfo;
using System.Text;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: CveIndex <release-notes-directory>");
    return;
}

var inputDir = args[0];
if (!Directory.Exists(inputDir) && (inputDir.EndsWith("release-notes/") || inputDir.EndsWith("release-notes")))
{
    // If the directory ends with 'release-notes/' or 'release-notes', try to find the parent directory
    inputDir = Path.GetDirectoryName(inputDir) ?? throw new InvalidOperationException("Invalid release notes directory path.");
}

var historyDir = Path.Combine(inputDir, "monthly");
if (!Directory.Exists(historyDir))
{
    Console.Error.WriteLine($"Directory '{historyDir}' does not exist.");
    return;
}

foreach (var file in Directory.GetFiles(historyDir, "index.json", SearchOption.AllDirectories))
{
    var parentDir = Path.GetDirectoryName(file)!;
    using var targetStream = File.Create(Path.Combine(parentDir, "README.md"));
    using var writer = new StreamWriter(targetStream);
    using var stream = File.OpenRead(file);

    var releaseCalendar = JsonSerializer.Deserialize<ReleaseHistory>(stream, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower
    });

    if (releaseCalendar == null)
    {
        Console.Error.WriteLine($"Failed to deserialize {file}");
        continue;
    }

    writer.WriteLine($"# {releaseCalendar.Year} Release Calendar");

    foreach (var releaseDays in releaseCalendar.ReleaseDays)
    {
        writer.WriteLine();
        writer.WriteLine($"## {releaseDays.Date:MMMM dd, yyyy}");
        writer.WriteLine();

        if (releaseDays.Releases.Count == 0)
        {
            writer.WriteLine("No releases.");
            continue;
        }

        foreach (var release in releaseDays.Releases)
        {
            writer.WriteLine($"- **{release.Version}**  {GetSecurityLabel(release)}");
        }

    }

}

string GetSecurityLabel(Release release)
{
    if (!release.Security)
        return string.Empty;

    // Map counts to severity labels
    string[] severities = ["Critical", "High", "Medium", "Low"];

    // Return empty when no severity data
    if (release.Severity == null || release.Severity.Length == 0)
        return string.Empty;

    // Build formatted parts
    var parts = release.Severity
        .Select((count, index) => $"{severities[index]}: {count}")
        .ToList();

    return parts.Any()
        ? string.Join("; ", parts)
        : string.Empty;
}

record ReleaseHistory(string Year, List<ReleaseDays> ReleaseDays);

record ReleaseDays(DateOnly Date, int Month, int Day, List<Release> Releases)
{
    public string? CveJson { get; set; }
};

record Release(string Version, bool Security)
{
    public int[]? Severity { get; set; }
};
