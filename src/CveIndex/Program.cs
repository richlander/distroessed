
using System.Text.Json;
using DotnetRelease;

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


var releaseIndex = Path.Combine(inputDir, "releases-index.json");
if (!File.Exists(releaseIndex))
{
    Console.Error.WriteLine($"Error: file '{releaseIndex}' does not exist.");
    return;
}

DateOnly firstDate = DateOnly.Parse("2023-11-14");
string calendarDirectory = Path.Combine(inputDir, "monthly");
Stream releaseIndexStream = File.OpenRead(releaseIndex);
var majorReleasesIndex = await ReleaseNotes.GetMajorReleasesIndex(releaseIndexStream) ?? throw new InvalidOperationException("Failed to load major releases index.");
Dictionary<string, List<Release>> releasesByDate = [];

foreach (var release in majorReleasesIndex.ReleasesIndex)
{
    if (release.LatestReleaseDate < firstDate)
    {
        continue; // Skip releases before the specified date
    }
    Console.WriteLine($"Release: {release.ChannelVersion}, Date: {release.LatestReleaseDate}");
    await ProcessVersionAsync(release.ChannelVersion, inputDir, firstDate, releasesByDate);
}

Dictionary<string, ReleaseCalendar> releaseCalendars = new();

foreach (var kvp in releasesByDate)
{
    var dateParts = kvp.Key.Split('-');
    if (dateParts.Length != 3)
    {
        continue; // Skip invalid dates
    }

    var year = dateParts[0];
    var yearInt = int.Parse(year);
    var month = int.Parse(dateParts[1]);
    var day = int.Parse(dateParts[2]);

    if (!releaseCalendars.ContainsKey(year))
    {
        releaseCalendars[year] = new ReleaseCalendar(year, new List<ReleaseDays>());
    }

    var relativePath = Path.Combine(year, month.ToString("D2"), "cve.json");
    var releaseEntry = new ReleaseDays(DateOnly.FromDateTime(DateTime.Parse(kvp.Key)), month, day, kvp.Value);
    releaseCalendars[year].ReleaseDays.Add(releaseEntry);
    var cveJson = Path.Combine(calendarDirectory, relativePath);
    if (File.Exists(cveJson))
    {
        releaseEntry.CveJson = relativePath;
    }
}

JsonSerializerOptions options = new()
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower
};

foreach (var calendar in releaseCalendars.Values)
{
    calendar.ReleaseDays.Sort((a, b) => a.Date.CompareTo(b.Date));
    var json = JsonSerializer.Serialize(calendar, options);
    File.WriteAllText(Path.Combine(calendarDirectory, calendar.Year, "index.json"), $"{json}\n");
    Console.WriteLine($"Created calendar for {calendar.Year} with {calendar.ReleaseDays.Count} months.");
}


async Task ProcessVersionAsync(string version, string baseDirectory, DateOnly firstDate, Dictionary<string, List<Release>> releasesByDate)
{
    var releaseJson = Path.Combine(baseDirectory, version, "releases.json");
    if (!File.Exists(releaseJson))
    {
        Console.Error.WriteLine($"Error: file '{releaseJson}' does not exist.");
        return;
    }

    var majorReleaseStream = File.OpenRead(releaseJson);
    var majorRelease = await ReleaseNotes.GetMajorRelease(majorReleaseStream) ?? throw new InvalidOperationException($"Failed to load release for version {version}.");

    foreach (var release in majorRelease.Releases)
    {
        if (release.ReleaseDate < firstDate || release.ReleaseVersion.Contains("0-"))
        {
            continue; // Skip releases before the specified date
        }

        if (!releasesByDate.ContainsKey(release.ReleaseDate.ToString("yyyy-MM-dd")))
        {
            releasesByDate[release.ReleaseDate.ToString("yyyy-MM-dd")] = [];
        }

        var releaseEntry = new Release(release.ReleaseVersion, release.Security);
        releasesByDate[release.ReleaseDate.ToString("yyyy-MM-dd")].Add(releaseEntry);
    }
}

record ReleaseCalendar(string Year, List<ReleaseDays> ReleaseDays);

record ReleaseDays(DateOnly Date, int Month, int Day, List<Release> Releases)
{
    public string? CveJson { get; set; }
};

record Release(string Version, bool Security);
