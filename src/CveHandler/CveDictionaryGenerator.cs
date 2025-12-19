using System.Globalization;
using DotnetRelease.Security;

namespace CveHandler;

/// <summary>
/// Generates lookup dictionaries from CVE records for efficient querying
/// </summary>
public static class CveDictionaryGenerator
{
    /// <summary>
    /// Comparer that sorts strings with numeric ordering (e.g., "2" before "10")
    /// </summary>
    private static readonly StringComparer NumericComparer =
        StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
    /// <summary>
    /// Generates all lookup dictionaries from CVE records
    /// </summary>
    public static GeneratedDictionaries GenerateAll(CveRecords cveRecords)
    {
        var productName = new Dictionary<string, string>();
        var productCves = new Dictionary<string, List<string>>();
        var packageCves = new Dictionary<string, List<string>>();
        var cveReleases = new Dictionary<string, List<string>>();
        var releaseCves = new Dictionary<string, List<string>>();
        var severityCves = InitializeSeverityDictionary();

        var validCveIds = new HashSet<string>(cveRecords.Disclosures.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);

        foreach (var product in cveRecords.Products)
        {
            if (!productName.ContainsKey(product.Name))
            {
                productName[product.Name] = ProductNameHelper.GetDisplayName(product.Name);
            }

            if (validCveIds.Contains(product.CveId))
            {
                if (!productCves.ContainsKey(product.Name))
                {
                    productCves[product.Name] = new List<string>();
                }
                if (!productCves[product.Name].Contains(product.CveId))
                {
                    productCves[product.Name].Add(product.CveId);
                }

                if (!string.IsNullOrEmpty(product.Release))
                {
                    string release = product.Release;

                    if (!cveReleases.ContainsKey(product.CveId))
                    {
                        cveReleases[product.CveId] = new List<string>();
                    }
                    if (!cveReleases[product.CveId].Contains(release))
                    {
                        cveReleases[product.CveId].Add(release);
                    }

                    if (!releaseCves.ContainsKey(release))
                    {
                        releaseCves[release] = new List<string>();
                    }
                    if (!releaseCves[release].Contains(product.CveId))
                    {
                        releaseCves[release].Add(product.CveId);
                    }
                }
            }
        }

        foreach (var package in cveRecords.Packages)
        {
            if (validCveIds.Contains(package.CveId))
            {
                if (!packageCves.ContainsKey(package.Name))
                {
                    packageCves[package.Name] = new List<string>();
                }
                if (!packageCves[package.Name].Contains(package.CveId))
                {
                    packageCves[package.Name].Add(package.CveId);
                }

                if (!string.IsNullOrEmpty(package.Release))
                {
                    string release = package.Release;

                    if (!cveReleases.ContainsKey(package.CveId))
                    {
                        cveReleases[package.CveId] = new List<string>();
                    }
                    if (!cveReleases[package.CveId].Contains(release))
                    {
                        cveReleases[package.CveId].Add(release);
                    }

                    if (!releaseCves.ContainsKey(release))
                    {
                        releaseCves[release] = new List<string>();
                    }
                    if (!releaseCves[release].Contains(package.CveId))
                    {
                        releaseCves[release].Add(package.CveId);
                    }
                }
            }
        }

        foreach (var disclosure in cveRecords.Disclosures)
        {
            AddSeverityMappings(severityCves, disclosure.Id, disclosure.Cvss.Severity);
        }

        // Sort CVE ID lists with numeric ordering (CVE-2025-55247 before CVE-2025-55315)
        foreach (var list in productCves.Values)
            list.Sort(NumericComparer);
        foreach (var list in packageCves.Values)
            list.Sort(NumericComparer);
        // Sort version lists with numeric ordering (8.0 before 9.0 before 10.0)
        foreach (var list in cveReleases.Values)
            list.Sort(NumericComparer);
        foreach (var list in releaseCves.Values)
            list.Sort(NumericComparer);

        return new GeneratedDictionaries(
            // CVE IDs as keys - use numeric ordering
            CveReleases: cveReleases.OrderBy(k => k.Key, NumericComparer).ToDictionary(k => k.Key, v => (IList<string>)v.Value),
            ProductCves: productCves.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => (IList<string>)v.Value),
            PackageCves: packageCves.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => (IList<string>)v.Value),
            ProductName: productName.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => v.Value),
            // Version numbers as keys - use numeric ordering
            ReleaseCves: releaseCves.OrderBy(k => k.Key, NumericComparer).ToDictionary(k => k.Key, v => (IList<string>)v.Value),
            SeverityCves: FinalizeSeverityDictionary(severityCves)
        );
    }

    /// <summary>
    /// Generates CVE-to-commits mapping dictionary
    /// </summary>
    public static IDictionary<string, IList<string>> GenerateCommits(CveRecords cveRecords)
    {
        var cveCommits = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        var validCommits = cveRecords.Commits is not null 
            ? new HashSet<string>(cveRecords.Commits.Keys, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var validCveIds = new HashSet<string>(cveRecords.Disclosures.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);

        foreach (var product in cveRecords.Products)
        {
            if (validCveIds.Contains(product.CveId) && product.Commits is not null && product.Commits.Count > 0)
            {
                if (!cveCommits.ContainsKey(product.CveId))
                {
                    cveCommits[product.CveId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
                foreach (var commit in product.Commits)
                {
                    if (validCommits.Contains(commit))
                    {
                        cveCommits[product.CveId].Add(commit);
                    }
                }
            }
        }

        foreach (var package in cveRecords.Packages)
        {
            if (validCveIds.Contains(package.CveId) && package.Commits is not null && package.Commits.Count > 0)
            {
                if (!cveCommits.ContainsKey(package.CveId))
                {
                    cveCommits[package.CveId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
                foreach (var commit in package.Commits)
                {
                    if (validCommits.Contains(commit))
                    {
                        cveCommits[package.CveId].Add(commit);
                    }
                }
            }
        }

        return cveCommits
            .OrderBy(k => k.Key, NumericComparer)
            .ToDictionary(
                k => k.Key,
                v => (IList<string>)v.Value.OrderBy(c => c).ToList()
            );
    }

    public static IDictionary<string, IList<string>> GenerateSeverityCves(IList<Cve> disclosures)
    {
        var severityCves = InitializeSeverityDictionary();

        foreach (var disclosure in disclosures)
        {
            AddSeverityMappings(severityCves, disclosure.Id, disclosure.Cvss.Severity);
        }

        return FinalizeSeverityDictionary(severityCves);
    }

    private static readonly List<string> SeverityLevels = new() { "CRITICAL", "HIGH", "MEDIUM", "LOW" };

    private static Dictionary<string, HashSet<string>> InitializeSeverityDictionary() =>
        SeverityLevels.ToDictionary(level => level, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

    private static void AddSeverityMappings(Dictionary<string, HashSet<string>> severityCves, string cveId, string severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
        {
            return;
        }

        var normalized = severity.Trim();
        var severityIndex = SeverityLevels.FindIndex(level => string.Equals(level, normalized, StringComparison.OrdinalIgnoreCase));
        if (severityIndex == -1)
        {
            return;
        }

        for (int i = severityIndex; i < SeverityLevels.Count; i++)
        {
            severityCves[SeverityLevels[i]].Add(cveId);
        }
    }

    private static IDictionary<string, IList<string>> FinalizeSeverityDictionary(Dictionary<string, HashSet<string>> severityCves) =>
        SeverityLevels.ToDictionary(
            level => level,
            level => (IList<string>)severityCves[level].OrderBy(id => id, NumericComparer).ToList(),
            StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Container for generated CVE lookup dictionaries
/// </summary>
public record GeneratedDictionaries(
    IDictionary<string, IList<string>> CveReleases,
    IDictionary<string, IList<string>> ProductCves,
    IDictionary<string, IList<string>> PackageCves,
    IDictionary<string, string> ProductName,
    IDictionary<string, IList<string>> ReleaseCves,
    IDictionary<string, IList<string>> SeverityCves
);
