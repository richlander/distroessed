using DotnetRelease.Graph;
using DotnetRelease.Security;

namespace CveHandler;

/// <summary>
/// Transforms CVE data between different formats (full disclosures to summaries)
/// </summary>
public static class CveTransformer
{
    /// <summary>
    /// Converts full CVE disclosure records to summary format for embedding in indexes
    /// </summary>
    public static List<CveRecordSummary> ToSummaries(CveRecords cveRecords)
    {
        if (cveRecords.Disclosures == null || cveRecords.Disclosures.Count == 0)
        {
            return [];
        }

        return cveRecords.Disclosures.Select(disclosure =>
        {
            // Find affected products and packages
            var affectedProducts = cveRecords.ProductCves?
                .Where(kv => kv.Value.Contains(disclosure.Id))
                .Select(kv => kv.Key)
                .ToList();

            var affectedPackages = cveRecords.PackageCves?
                .Where(kv => kv.Value.Contains(disclosure.Id))
                .Select(kv => kv.Key)
                .ToList();

            // Build links
            var links = new Dictionary<string, object>();
            var announcementUrl = disclosure.References?.FirstOrDefault();
            if (announcementUrl != null)
            {
                links["announcement"] = new HalLink(announcementUrl)
                {
                    Title = $"Announcement for {disclosure.Id}"
                };
            }

            // Build fix commit links
            var fixes = new List<CommitLink>();
            if (cveRecords.CveCommits?.TryGetValue(disclosure.Id, out var commitHashes) == true)
            {
                foreach (var hash in commitHashes)
                {
                    if (cveRecords.Commits?.TryGetValue(hash, out var commitInfo) == true)
                    {
                        var release = cveRecords.Products?
                            .Where(p => p.CveId == disclosure.Id && p.Commits.Contains(hash))
                            .Select(p => p.Release)
                            .FirstOrDefault();

                        if (string.IsNullOrEmpty(release))
                        {
                            release = cveRecords.Packages?
                                .Where(p => p.CveId == disclosure.Id && p.Commits.Contains(hash))
                                .Select(p => p.Release)
                                .FirstOrDefault();
                        }

                        var repoFullName = $"{commitInfo.Org}/{commitInfo.Repo}";
                        fixes.Add(new CommitLink(
                            commitInfo.Url,
                            repoFullName,
                            commitInfo.Branch)
                        {
                            Title = $"Fix commit in {commitInfo.Repo} ({commitInfo.Branch})",
                            Release = !string.IsNullOrEmpty(release) ? release : null
                        });
                    }
                }
            }

            return new CveRecordSummary(disclosure.Id, disclosure.Problem)
            {
                Links = links.Count > 0 ? links : null,
                Fixes = fixes.Count > 0 ? fixes : null,
                CvssScore = disclosure.Cvss.Score,
                CvssSeverity = disclosure.Cvss.Severity,
                DisclosureDate = disclosure.Timeline.Disclosure.Date,
                AffectedReleases = cveRecords.CveReleases?.TryGetValue(disclosure.Id, out var releases) == true 
                    ? releases 
                    : null,
                AffectedProducts = affectedProducts?.Count > 0 ? affectedProducts : null,
                AffectedPackages = affectedPackages?.Count > 0 ? affectedPackages : null,
                Platforms = disclosure.Platforms
            };
        }).ToList();
    }

    /// <summary>
    /// Extracts just the CVE IDs from CVE records
    /// </summary>
    public static List<string> ExtractCveIds(CveRecords? cveRecords)
    {
        if (cveRecords?.Disclosures == null || cveRecords.Disclosures.Count == 0)
        {
            return [];
        }

        return cveRecords.Disclosures.Select(d => d.Id).ToList();
    }

    /// <summary>
    /// Filters CVE records to only include those affecting a specific release version
    /// </summary>
    public static CveRecords? FilterByRelease(CveRecords? cveRecords, string releaseVersion)
    {
        if (cveRecords == null || cveRecords.Disclosures.Count == 0)
        {
            return null;
        }

        // Get CVE IDs for this release
        var cveIds = cveRecords.ReleaseCves?.TryGetValue(releaseVersion, out var ids) == true
            ? ids.ToHashSet()
            : new HashSet<string>();

        if (cveIds.Count == 0)
        {
            return null;
        }

        // Filter disclosures
        var filteredDisclosures = cveRecords.Disclosures
            .Where(d => cveIds.Contains(d.Id))
            .ToList();

        if (filteredDisclosures.Count == 0)
        {
            return null;
        }

        // Filter products
        var filteredProducts = cveRecords.Products
            .Where(p => cveIds.Contains(p.CveId) && p.Release == releaseVersion)
            .ToList();

        // Filter packages
        var filteredPackages = cveRecords.Packages
            .Where(p => cveIds.Contains(p.CveId) && p.Release == releaseVersion)
            .ToList();

        // Build filtered commits dictionary
        var neededCommitHashes = filteredProducts.SelectMany(p => p.Commits)
            .Concat(filteredPackages.SelectMany(p => p.Commits))
            .ToHashSet();

        var filteredCommits = cveRecords.Commits?
            .Where(kv => neededCommitHashes.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        // Build filtered product CVE mapping
        var filteredProductCves = filteredProducts
            .GroupBy(p => p.Name)
            .ToDictionary(
                g => g.Key,
                g => (IList<string>)g.Select(p => p.CveId).Distinct().ToList()
            );

        // Build filtered package CVE mapping
        var filteredPackageCves = filteredPackages
            .GroupBy(p => p.Name)
            .ToDictionary(
                g => g.Key,
                g => (IList<string>)g.Select(p => p.CveId).Distinct().ToList()
            );

        // Build CVE-specific mappings
        var filteredCveCommits = cveRecords.CveCommits?
            .Where(kv => cveIds.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return new CveRecords(
            cveRecords.LastUpdated,
            $"CVEs affecting {releaseVersion}",
            filteredDisclosures,
            filteredProducts,
            filteredPackages,
            filteredCommits,
            cveRecords.ProductName,
            filteredProductCves,
            filteredPackageCves,
            new Dictionary<string, IList<string>> { [releaseVersion] = [.. cveIds] },
            cveRecords.CveReleases?.Where(kv => cveIds.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value),
            filteredCveCommits
        );
    }

    /// <summary>
    /// Validates that CVE records match what's in a release (from releases.json)
    /// Logs warnings for any mismatches
    /// </summary>
    /// <param name="releaseVersion">Version being validated (e.g., "9.0.3")</param>
    /// <param name="cveIdsFromRelease">CVE IDs from releases.json</param>
    /// <param name="cveIdsFromCveJson">CVE IDs from cve.json (filtered)</param>
    public static void ValidateCveData(string releaseVersion, IReadOnlyList<string>? cveIdsFromRelease, IReadOnlyList<string>? cveIdsFromCveJson)
    {
        var releaseCves = cveIdsFromRelease?.ToHashSet() ?? new HashSet<string>();
        var cveJsonCves = cveIdsFromCveJson?.ToHashSet() ?? new HashSet<string>();

        if (releaseCves.Count == 0 && cveJsonCves.Count == 0)
        {
            return; // No CVEs in either source - OK
        }

        var inReleaseOnly = releaseCves.Except(cveJsonCves).ToList();
        var inCveJsonOnly = cveJsonCves.Except(releaseCves).ToList();

        if (inReleaseOnly.Count > 0)
        {
            Console.WriteLine($"Warning: {releaseVersion} - CVE IDs in releases.json but not in cve.json: {string.Join(", ", inReleaseOnly)}");
        }

        if (inCveJsonOnly.Count > 0)
        {
            Console.WriteLine($"Warning: {releaseVersion} - CVE IDs in cve.json but not in releases.json: {string.Join(", ", inCveJsonOnly)}");
        }
    }
}
