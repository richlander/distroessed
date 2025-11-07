using DotnetRelease.Cves;

namespace DotnetRelease;

/// <summary>
/// Helper methods for filtering CVE records by version, platform, and other criteria.
/// </summary>
public static class CveFilter
{
    /// <summary>
    /// Filters CVEs to only those affecting a specific .NET version.
    /// </summary>
    /// <param name="cveRecords">Collection of CVE record documents</param>
    /// <param name="version">Major version to filter by (e.g., "8.0", "9.0")</param>
    /// <returns>CVEs affecting the specified version</returns>
    public static IEnumerable<Cve> FilterByVersion(IEnumerable<CveRecords> cveRecords, string version)
    {
        ArgumentNullException.ThrowIfNull(cveRecords);
        ArgumentNullException.ThrowIfNullOrEmpty(version);

        var affectedCveIds = new HashSet<string>();

        foreach (var records in cveRecords)
        {
            // Find products that match the version
            var matchingProducts = records.Products
                .Where(p => p.Release == version);

            foreach (var product in matchingProducts)
            {
                affectedCveIds.Add(product.CveId);
            }
        }

        // Return CVEs with IDs in the affected set
        return cveRecords
            .SelectMany(r => r.Cves)
            .Where(c => affectedCveIds.Contains(c.Id))
            .DistinctBy(c => c.Id);
    }

    /// <summary>
    /// Filters CVEs to only those affecting a specific platform.
    /// </summary>
    /// <param name="cves">Collection of CVEs</param>
    /// <param name="platform">Platform to filter by (e.g., "windows", "linux", "macos")</param>
    /// <param name="includeAll">Whether to include CVEs marked as affecting "all" platforms (default: true)</param>
    /// <returns>CVEs affecting the specified platform</returns>
    public static IEnumerable<Cve> FilterByPlatform(IEnumerable<Cve> cves, string platform, bool includeAll = true)
    {
        ArgumentNullException.ThrowIfNull(cves);
        ArgumentNullException.ThrowIfNullOrEmpty(platform);

        return cves.Where(c =>
            c.Platforms.Any(p => p.Equals(platform, StringComparison.OrdinalIgnoreCase)) ||
            (includeAll && c.Platforms.Any(p => p.Equals("all", StringComparison.OrdinalIgnoreCase)))
        );
    }

    /// <summary>
    /// Filters CVEs to only those affecting specific platforms.
    /// </summary>
    /// <param name="cves">Collection of CVEs</param>
    /// <param name="platforms">Platforms to filter by</param>
    /// <param name="includeAll">Whether to include CVEs marked as affecting "all" platforms (default: true)</param>
    /// <returns>CVEs affecting any of the specified platforms</returns>
    public static IEnumerable<Cve> FilterByPlatforms(IEnumerable<Cve> cves, IEnumerable<string> platforms, bool includeAll = true)
    {
        ArgumentNullException.ThrowIfNull(cves);
        ArgumentNullException.ThrowIfNull(platforms);

        var platformSet = new HashSet<string>(platforms, StringComparer.OrdinalIgnoreCase);

        return cves.Where(c =>
            c.Platforms.Any(p => platformSet.Contains(p)) ||
            (includeAll && c.Platforms.Any(p => p.Equals("all", StringComparison.OrdinalIgnoreCase)))
        );
    }

    /// <summary>
    /// Filters CVEs by severity level.
    /// </summary>
    /// <param name="cves">Collection of CVEs</param>
    /// <param name="severity">Severity level (e.g., "Critical", "High", "Medium", "Low")</param>
    /// <returns>CVEs with the specified severity</returns>
    public static IEnumerable<Cve> FilterBySeverity(IEnumerable<Cve> cves, string severity)
    {
        ArgumentNullException.ThrowIfNull(cves);
        ArgumentNullException.ThrowIfNullOrEmpty(severity);

        return cves.Where(c => c.Severity.Equals(severity, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters CVEs to only Critical and High severity.
    /// </summary>
    public static IEnumerable<Cve> FilterHighSeverity(IEnumerable<Cve> cves)
    {
        ArgumentNullException.ThrowIfNull(cves);
        return cves.Where(c =>
            c.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
            c.Severity.Equals("High", StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// Combines filtering by version and platform - typical workflow.
    /// </summary>
    /// <param name="cveRecords">Collection of CVE record documents</param>
    /// <param name="version">Major version to filter by (e.g., "8.0")</param>
    /// <param name="platform">Platform to filter by (e.g., "windows")</param>
    /// <param name="includeAllPlatforms">Whether to include CVEs affecting "all" platforms</param>
    /// <returns>CVEs affecting the specified version and platform</returns>
    public static IEnumerable<Cve> FilterByVersionAndPlatform(
        IEnumerable<CveRecords> cveRecords,
        string version,
        string platform,
        bool includeAllPlatforms = true)
    {
        var versionFiltered = FilterByVersion(cveRecords, version);
        return FilterByPlatform(versionFiltered, platform, includeAllPlatforms);
    }
}
