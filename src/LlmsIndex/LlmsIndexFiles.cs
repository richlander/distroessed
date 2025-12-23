using System.Globalization;
using System.Text.Json;
using DotnetRelease;
using DotnetRelease.Graph;
using DotnetRelease.Summary;

namespace LlmsIndex;

// Alias to avoid conflict with DotnetRelease.Graph.LlmsIndex record
using LlmsIndexRecord = DotnetRelease.Graph.LlmsIndex;

public static class LlmsIndexFiles
{
    public static async Task GenerateAsync(
        string inputDir,
        string outputDir,
        List<MajorReleaseSummary> summaries,
        ReleaseHistory releaseHistory)
    {
        var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);

        // Load partial _llms.json for hand-maintained fields (ai_note, etc.)
        PartialLlmsIndex? partial = null;
        var partialPath = Path.Combine(inputDir, FileNames.PartialLlms);
        if (File.Exists(partialPath))
        {
            try
            {
                var partialJson = await File.ReadAllTextAsync(partialPath);
                partial = JsonSerializer.Deserialize<PartialLlmsIndex>(partialJson, LlmsIndexSerializerContext.Default.PartialLlmsIndex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to read {partialPath}: {ex.Message}");
            }
        }

        // Find supported releases (those with Supported = true)
        var supportedSummaries = summaries
            .Where(s => s.Lifecycle?.Supported == true)
            .OrderByDescending(s => s.MajorVersion, numericStringComparer)
            .ToList();

        var supportedReleases = supportedSummaries
            .Select(s => s.MajorVersion)
            .ToList();

        // Find latest stable release and latest LTS release
        var releaseData = summaries.Select(s => (s.MajorVersion, (Lifecycle?)s.Lifecycle));
        var latestVersion = ReleaseStability.FindLatestVersion(releaseData, numericStringComparer);
        var latestLtsVersion = ReleaseStability.FindLatestLtsVersion(releaseData, numericStringComparer);

        // Get the latest year from release history
        var latestYear = releaseHistory.Years.Keys
            .OrderByDescending(y => y, numericStringComparer)
            .FirstOrDefault();

        // Build latest patches collection
        var latestPatches = new List<LlmsPatchEntry>();
        foreach (var summary in supportedSummaries)
        {
            var latestPatch = summary.PatchReleases
                .OrderByDescending(p => p.ReleaseDate)
                .ThenByDescending(p => p.PatchVersion, numericStringComparer)
                .FirstOrDefault();

            if (latestPatch == null) continue;

            var releaseDate = new DateTimeOffset(latestPatch.ReleaseDate, TimeOnly.MinValue, TimeSpan.Zero);

            // Get SDK version from components
            var sdkVersion = latestPatch.Components?
                .Where(c => c.Name == "SDK")
                .Select(c => c.Version)
                .OrderByDescending(v => v, numericStringComparer)
                .FirstOrDefault();

            // Get CVE IDs for this patch (from cve.json via release history)
            IReadOnlyList<string>? cveIds = null;
            var cveRecords = await CveHandler.CveLoader.LoadCveRecordsForReleaseDateAsync(inputDir, releaseDate);
            if (cveRecords?.ReleaseCves != null && cveRecords.ReleaseCves.TryGetValue(summary.MajorVersion, out var cveIdsFromCveJson))
            {
                cveIds = cveIdsFromCveJson.ToList();
            }

            // Build patch entry links - self points to patch index
            // Note: No titles in embedded links - context established by parent, saves tokens for LLM consumers
            var patchDirPath = latestPatch.PatchDirPath ?? $"{summary.MajorVersion}/{latestPatch.PatchVersion}";
            var patchIndexPath = $"{patchDirPath}/{FileNames.Index}";
            var patchLinks = new Dictionary<string, HalLink>
            {
                [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{patchIndexPath}")
            };

            // Find latest security patch for this release (for quick hop when security=false)
            var latestSecurityPatch = summary.PatchReleases
                .Where(p => p.CveList?.Count > 0)
                .OrderByDescending(p => p.ReleaseDate)
                .ThenByDescending(p => p.PatchVersion, numericStringComparer)
                .FirstOrDefault();

            if (latestSecurityPatch != null)
            {
                var securityPatchDirPath = latestSecurityPatch.PatchDirPath ?? $"{summary.MajorVersion}/{latestSecurityPatch.PatchVersion}";
                var securityPatchIndexPath = $"{securityPatchDirPath}/{FileNames.Index}";
                patchLinks[LinkRelations.LatestSecurityPatch] = new HalLink($"{Location.GitHubBaseUri}{securityPatchIndexPath}");
            }

            // Add release-major link to navigate to the major version index
            patchLinks[LinkRelations.Major] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Index}");

            // Add downloads link for versions that support SDK hive (8.0+)
            var majorVersionParts = summary.MajorVersion.Split('.');
            if (majorVersionParts.Length >= 1 && int.TryParse(majorVersionParts[0], out var majorNum) && majorNum >= 8)
            {
                patchLinks[LinkRelations.Downloads] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Directories.Downloads}/{FileNames.Index}");
            }

            // Add major-manifest link for direct access to reference data (compatibility, TFMs, OS support)
            patchLinks[LinkRelations.MajorManifest] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Manifest}");

            // Skip entries without complete lifecycle data (ReleaseType is nullable, Phase is not)
            if (summary.Lifecycle == null || summary.Lifecycle.ReleaseType == null)
            {
                continue;
            }

            // Skip entries without SDK version (shouldn't happen for supported releases)
            if (sdkVersion == null)
            {
                continue;
            }

            // Use current patch as fallback if no security patches exist yet
            var latestSecurityVersion = latestSecurityPatch?.PatchVersion ?? latestPatch.PatchVersion;
            var latestSecurityDateOnly = latestSecurityPatch?.ReleaseDate ?? latestPatch.ReleaseDate;
            var latestSecurityDate = new DateTimeOffset(latestSecurityDateOnly, TimeOnly.MinValue, TimeSpan.Zero);

            var patchEntry = new LlmsPatchEntry(
                latestPatch.PatchVersion,
                summary.MajorVersion,
                summary.Lifecycle.ReleaseType.Value,
                cveIds?.Count > 0,
                summary.Lifecycle.Phase,
                summary.Lifecycle.Supported,
                sdkVersion,
                latestSecurityVersion,
                latestSecurityDate,
                HalHelpers.OrderLinks(patchLinks));

            latestPatches.Add(patchEntry);
        }

        // Find latest month and latest security month for links
        string? latestMonthYear = null;
        string? latestMonthNumber = null;
        string? latestSecurityMonthYear = null;
        string? latestSecurityMonthNumber = null;

        var allMonths = releaseHistory.Years
            .SelectMany(y => y.Value.Months.Select(m => (Year: y.Key, Month: m.Key, Data: m.Value)))
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .ToList();

        // First entry is the latest month
        if (allMonths.Count > 0)
        {
            latestMonthYear = allMonths[0].Year;
            latestMonthNumber = allMonths[0].Month;
        }

        foreach (var (yearKey, monthKey, monthData) in allMonths)
        {
            // Check if this month has any CVEs
            var hasCves = monthData.Days.Values
                .SelectMany(d => d.Releases)
                .Any(r => r.CveList?.Count > 0);

            if (hasCves)
            {
                latestSecurityMonthYear = yearKey;
                latestSecurityMonthNumber = monthKey;
                break;
            }
        }

        // Compute latest patch date and latest security patch date from embedded patches
        DateTimeOffset? latestPatchDate = null;
        DateTimeOffset? latestSecurityPatchDate = null;

        foreach (var summary in supportedSummaries)
        {
            var latestPatch = summary.PatchReleases
                .OrderByDescending(p => p.ReleaseDate)
                .ThenByDescending(p => p.PatchVersion, numericStringComparer)
                .FirstOrDefault();

            if (latestPatch != null)
            {
                var patchDate = new DateTimeOffset(latestPatch.ReleaseDate, TimeOnly.MinValue, TimeSpan.Zero);
                if (latestPatchDate == null || patchDate > latestPatchDate)
                {
                    latestPatchDate = patchDate;
                }
            }

            var latestSecurityPatch = summary.PatchReleases
                .Where(p => p.CveList?.Count > 0)
                .OrderByDescending(p => p.ReleaseDate)
                .ThenByDescending(p => p.PatchVersion, numericStringComparer)
                .FirstOrDefault();

            if (latestSecurityPatch != null)
            {
                var securityPatchDate = new DateTimeOffset(latestSecurityPatch.ReleaseDate, TimeOnly.MinValue, TimeSpan.Zero);
                if (latestSecurityPatchDate == null || securityPatchDate > latestSecurityPatchDate)
                {
                    latestSecurityPatchDate = securityPatchDate;
                }
            }
        }

        // Build root links
        var links = new Dictionary<string, HalLink>
        {
            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Llms}")
        };

        // Add latest links
        if (latestVersion != null)
        {
            links[LinkRelations.LatestMajor] = new HalLink($"{Location.GitHubBaseUri}{latestVersion}/{FileNames.Index}")
            {
                Title = $"Latest major release - .NET {latestVersion}"
            };
        }

        if (latestLtsVersion != null)
        {
            links[LinkRelations.LatestLtsMajor] = new HalLink($"{Location.GitHubBaseUri}{latestLtsVersion}/{FileNames.Index}")
            {
                Title = $"Latest LTS major release - .NET {latestLtsVersion}"
            };
        }

        if (latestYear != null)
        {
            links[LinkRelations.LatestYear] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{latestYear}/{FileNames.Index}")
            {
                Title = $"Latest year - {latestYear}"
            };
        }

        // Add latest-month link
        if (latestMonthYear != null && latestMonthNumber != null)
        {
            links[LinkRelations.LatestMonth] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{latestMonthYear}/{latestMonthNumber}/{FileNames.Index}")
            {
                Title = $"Latest month - {IndexTitles.FormatMonthYear(latestMonthYear, latestMonthNumber)}"
            };
        }

        // Add latest-security-month link
        if (latestSecurityMonthYear != null && latestSecurityMonthNumber != null)
        {
            links[LinkRelations.LatestSecurityMonth] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{latestSecurityMonthYear}/{latestSecurityMonthNumber}/{FileNames.Index}")
            {
                Title = $"Latest security month - {IndexTitles.FormatMonthYear(latestSecurityMonthYear, latestSecurityMonthNumber)}"
            };
        }

        // Add releases-index and timeline-index links
        links[LinkRelations.Root] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Index}")
        {
            Title = ".NET Release Index"
        };

        links[LinkRelations.Timeline] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{FileNames.Index}")
        {
            Title = ".NET Release Timeline Index"
        };

        // Build required_pre_read URL (skill file in release-notes/)
        var requiredPreRead = $"{Location.GitHubBaseUri}skills/dotnet-releases/SKILL.md";

        // Merge additional links from partial
        if (partial?.Links != null)
        {
            foreach (var (key, link) in partial.Links)
            {
                if (key == HalTerms.Self) continue;
                links[key] = link;
            }
        }

        // Build the LlmsIndex
        var llmsIndex = new LlmsIndexRecord(
            ReleaseKind.Llms,
            partial?.Title ?? ".NET Release Index for AI")
        {
            AiNote = partial?.AiNote ?? "ALWAYS read required_pre_read first. HAL graph—follow _links only, never construct URLs.",
            HumanNote = partial?.HumanNote,
            RequiredPreRead = requiredPreRead,
            LatestMajor = latestVersion,
            LatestLtsMajor = latestLtsVersion,
            LatestPatchDate = latestPatchDate,
            LatestSecurityPatchDate = latestSecurityPatchDate,
            SupportedMajorReleases = supportedReleases,
            Links = HalHelpers.OrderLinks(links),
            Embedded = new LlmsIndexEmbedded
            {
                Patches = latestPatches.Count > 0 ? latestPatches : null
            }
        };

        // Serialize
        var llmsIndexJson = JsonSerializer.Serialize(
            llmsIndex,
            LlmsIndexSerializerContext.Default.LlmsIndex);

        // Write to file
        var llmsIndexPath = Path.Combine(outputDir, FileNames.Llms);
        var finalJson = llmsIndexJson + '\n';
        await File.WriteAllTextAsync(llmsIndexPath, finalJson);

        Console.WriteLine($"Generated {llmsIndexPath}");
    }
}
