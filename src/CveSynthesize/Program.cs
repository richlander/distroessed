using System.Text.Json;
using DotnetRelease.Security;

// CveSynthesize - Synthesize CVE JSON files for historical releases
// 
// This tool creates cve.json files in the timeline structure for releases
// before 2023-11 (when timeline CVE files were first created).
//
// It extracts CVE data from releases.json files and creates properly
// structured cve.json files that can be used by VersionIndex and other tools.
//
// Usage:
//   CveSynthesize <release-notes-path>
//
// Example:
//   CveSynthesize ~/git/core/release-notes

Console.WriteLine("CveSynthesize - Synthesize historical CVE JSON files");
Console.WriteLine();

if (args.Length < 1)
{
    Console.WriteLine("Usage: CveSynthesize <release-notes-path>");
    Console.WriteLine("Example: CveSynthesize ~/git/core/release-notes");
    return 1;
}

var releaseNotesPath = args[0];
if (!Directory.Exists(releaseNotesPath))
{
    Console.WriteLine($"Error: Directory not found: {releaseNotesPath}");
    return 1;
}

Console.WriteLine($"Release notes path: {releaseNotesPath}");
Console.WriteLine();

// Find the earliest existing timeline cve.json to determine cutoff date
var timelinePath = Path.Combine(releaseNotesPath, "timeline");
var cutoffDate = FindEarliestTimelineCveDate(timelinePath);

if (cutoffDate != null)
{
    Console.WriteLine($"Earliest existing timeline CVE file: {cutoffDate:yyyy-MM}");
    Console.WriteLine($"Will synthesize CVE files for releases before this date");
}
else
{
    Console.WriteLine("No existing timeline CVE files found, will process all releases");
}
Console.WriteLine();

// Scan releases.json files for CVE data
var releasesByCveMonth = new Dictionary<string, List<ReleaseWithCves>>();

foreach (var majorVersionDir in Directory.GetDirectories(releaseNotesPath))
{
    var majorVersion = Path.GetFileName(majorVersionDir);
    
    // Skip non-version directories
    if (!majorVersion.Contains('.') || majorVersion == "timeline")
    {
        continue;
    }
    
    var releasesJsonPath = Path.Combine(majorVersionDir, "releases.json");
    if (!File.Exists(releasesJsonPath))
    {
        continue;
    }
    
    Console.WriteLine($"Processing {majorVersion}/releases.json...");
    
    try
    {
        var releasesJson = await File.ReadAllTextAsync(releasesJsonPath);
        var releasesDoc = JsonDocument.Parse(releasesJson);
        
        if (!releasesDoc.RootElement.TryGetProperty("releases", out var releasesArray))
        {
            continue;
        }
        
        foreach (var release in releasesArray.EnumerateArray())
        {
            if (!release.TryGetProperty("release-date", out var releaseDateProp))
            {
                continue;
            }
            
            var releaseDateStr = releaseDateProp.GetString();
            if (string.IsNullOrEmpty(releaseDateStr))
            {
                continue;
            }
            
            if (!DateOnly.TryParse(releaseDateStr, out var releaseDate))
            {
                continue;
            }
            
            // Skip if after cutoff date
            if (cutoffDate != null && releaseDate >= cutoffDate.Value)
            {
                continue;
            }
            
            // Check for CVEs
            if (!release.TryGetProperty("cve-list", out var cveListArray) || cveListArray.GetArrayLength() == 0)
            {
                continue;
            }
            
            if (!release.TryGetProperty("release-version", out var versionProp))
            {
                continue;
            }
            
            var version = versionProp.GetString();
            if (string.IsNullOrEmpty(version))
            {
                continue;
            }
            
            // Extract CVE IDs
            var cveIds = new List<string>();
            foreach (var cveEntry in cveListArray.EnumerateArray())
            {
                if (cveEntry.TryGetProperty("cve-id", out var cveIdProp))
                {
                    var cveId = cveIdProp.GetString();
                    if (!string.IsNullOrEmpty(cveId))
                    {
                        cveIds.Add(cveId);
                    }
                }
            }
            
            if (cveIds.Count == 0)
            {
                continue;
            }
            
            // Group by year-month
            var yearMonth = $"{releaseDate.Year:D4}-{releaseDate.Month:D2}";
            if (!releasesByCveMonth.ContainsKey(yearMonth))
            {
                releasesByCveMonth[yearMonth] = new List<ReleaseWithCves>();
            }
            
            releasesByCveMonth[yearMonth].Add(new ReleaseWithCves(
                version,
                majorVersion,
                releaseDate,
                cveIds));
            
            Console.WriteLine($"  Found {cveIds.Count} CVE(s) in {version} ({releaseDate:yyyy-MM-dd})");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error processing {releasesJsonPath}: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"Found {releasesByCveMonth.Count} month(s) with CVE data to synthesize");
Console.WriteLine();

// TODO: Generate cve.json files for each month
// This will be implemented in the next phase

return 0;

static DateOnly? FindEarliestTimelineCveDate(string timelinePath)
{
    if (!Directory.Exists(timelinePath))
    {
        return null;
    }
    
    DateOnly? earliest = null;
    
    foreach (var yearDir in Directory.GetDirectories(timelinePath))
    {
        var year = Path.GetFileName(yearDir);
        if (!int.TryParse(year, out var yearNum))
        {
            continue;
        }
        
        foreach (var monthDir in Directory.GetDirectories(yearDir))
        {
            var month = Path.GetFileName(monthDir);
            if (!int.TryParse(month, out var monthNum))
            {
                continue;
            }
            
            var cveJsonPath = Path.Combine(monthDir, "cve.json");
            if (File.Exists(cveJsonPath))
            {
                var date = new DateOnly(yearNum, monthNum, 1);
                if (earliest == null || date < earliest.Value)
                {
                    earliest = date;
                }
            }
        }
    }
    
    return earliest;
}

record ReleaseWithCves(
    string Version,
    string MajorVersion,
    DateOnly ReleaseDate,
    List<string> CveIds);
