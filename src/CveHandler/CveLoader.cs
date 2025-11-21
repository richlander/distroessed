using System.Text.Json;
using DotnetRelease.Security;

namespace CveHandler;

/// <summary>
/// Loads CVE data from JSON files
/// </summary>
public static class CveLoader
{
    /// <summary>
    /// Loads CVE records from a cve.json file
    /// </summary>
    public static async Task<CveRecords?> LoadCveRecordsAsync(string cveJsonPath)
    {
        if (!File.Exists(cveJsonPath))
        {
            return null;
        }

        using var stream = File.OpenRead(cveJsonPath);
        return await JsonSerializer.DeserializeAsync<CveRecords>(stream, CveSerializerContext.Default.CveRecords);
    }

    /// <summary>
    /// Loads CVE records from a directory containing cve.json
    /// </summary>
    public static async Task<CveRecords?> LoadCveRecordsFromDirectoryAsync(string directoryPath)
    {
        var cveJsonPath = Path.Combine(directoryPath, "cve.json");
        return await LoadCveRecordsAsync(cveJsonPath);
    }

    /// <summary>
    /// Loads CVE records from the timeline directory for a specific release date
    /// </summary>
    /// <param name="releaseNotesRoot">Root path of release-notes directory</param>
    /// <param name="releaseDate">Release date to find CVE data for</param>
    /// <returns>CVE records for that month, or null if not found</returns>
    public static async Task<CveRecords?> LoadCveRecordsForReleaseDateAsync(string releaseNotesRoot, DateTimeOffset releaseDate)
    {
        var year = releaseDate.Year.ToString("D4");
        var month = releaseDate.Month.ToString("D2");
        var timelinePath = Path.Combine(releaseNotesRoot, "timeline", year, month);
        
        return await LoadCveRecordsFromDirectoryAsync(timelinePath);
    }
}
