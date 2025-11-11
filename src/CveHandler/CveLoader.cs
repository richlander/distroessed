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
}
