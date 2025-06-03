
using System.Text.Json;
using CveInfo;

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
            if (!string.IsNullOrEmpty(releaseDays.CveJson))
            {
                var cveFilePath = Path.Combine(historyDir, releaseDays.CveJson);
                if (File.Exists(cveFilePath))
                {
                    using Stream cveStream = File.OpenRead(cveFilePath);
                    var cveRecords = JsonSerializer.Deserialize<CveRecords>(cveStream, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower
                    });

                    writer.WriteLine();
                    writer.WriteLine("### CVE Information");
                    writer.WriteLine();
                    writer.WriteLine(cveRecords.Date);

                }

            }

            writer.WriteLine($"- **{release.Version}** {(release.Security ? "(Security)" : "")}");
        }

    }

}

record ReleaseHistory(string Year, List<ReleaseDays> ReleaseDays);

record ReleaseDays(DateOnly Date, int Month, int Day, List<Release> Releases)
{
    public string? CveJson { get; set; }
};

record Release(string Version, bool Security);
