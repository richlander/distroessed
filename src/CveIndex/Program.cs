
using System.Text.Json;
using CveIndex;
using CveInfo;
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
string historyDir = Path.Combine(inputDir, "monthly");
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
        releaseCalendars[year] = new ReleaseCalendar(year, []);
    }

    var relativePath = Path.Combine(year, month.ToString("D2"), "cve.json");
    var releaseDays = new ReleaseDay(DateOnly.FromDateTime(DateTime.Parse(kvp.Key)), month, day, kvp.Value);
    releaseCalendars[year].ReleaseDays.Add(releaseDays);
    var cveJson = Path.Combine(historyDir, relativePath);
    if (File.Exists(cveJson))
    {
        releaseDays.CveJson = relativePath;

        foreach (var release in kvp.Value)
        {
            CveRecords? cveRecords = null;
            if (!string.IsNullOrEmpty(releaseDays.CveJson))
            {
                var cveFilePath = Path.Combine(historyDir, releaseDays.CveJson);
                if (File.Exists(cveFilePath))
                {
                    using Stream cveStream = File.OpenRead(cveFilePath);
                    cveRecords = JsonSerializer.Deserialize<CveRecords>(cveStream, CveInfoSerializerContext.Default.CveRecords);
                }

                AddSeverity(release, cveRecords);
            }
        }

    }
}

foreach (var calendar in releaseCalendars.Values)
{
    calendar.ReleaseDays.Sort((a, b) => a.Date.CompareTo(b.Date));
    var json = JsonSerializer.Serialize(calendar, CveIndexSerializationContext.Default.ReleaseCalendar);
    File.WriteAllText(Path.Combine(historyDir, calendar.Year, "index.json"), $"{json}\n");
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

void AddSeverity(Release release, CveRecords? cveRecords)
{
    if (!release.Security || cveRecords is null)
        return;

    string twoPart = release.Version[..3];
    string[] severities = ["Critical", "High", "Medium", "Low"];
    List<int> severityList = [];

    // Build severity counts and format as "Severity: count"
    var sevCount = cveRecords.Packages
        .SelectMany(p => p.Affected)
        .Where(a => a.Family == twoPart)
        .Join(
            cveRecords.Records,
            a => a.CveId,
            r => r.Id,
            (_, r) => r.Severity
        )
        .GroupBy(sev => sev)
        .Select(g => new SevCount(g.Key ?? throw new Exception($"Missing severity: {release.Version}; Date: {cveRecords?.Date}"), g.Count()))
        .ToDictionary(s => s.Severity, s => s.Count);


    foreach (var severity in severities)
    {
        severityList.Add(sevCount.TryGetValue(severity, out var count) ? count : 0);
    }

    if (severityList.Count > 0)
    {
        release.Severity = severityList;
    }
}

