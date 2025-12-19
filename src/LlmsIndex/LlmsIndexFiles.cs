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
                patchLinks[LinkRelations.LatestSecurity] = new HalLink($"{Location.GitHubBaseUri}{securityPatchIndexPath}");
            }

            // Add release-major link to navigate to the major version index
            patchLinks[LinkRelations.ReleaseMajor] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Index}");

            // Add manifest link for direct access to reference data (compatibility, TFMs, OS support)
            patchLinks[LinkRelations.Manifest] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Manifest}");

            var patchEntry = new LlmsPatchEntry(latestPatch.PatchVersion, summary.MajorVersion)
            {
                ReleaseType = summary.Lifecycle?.ReleaseType,
                Date = releaseDate,
                Year = releaseDate.Year.ToString("D4"),
                Month = releaseDate.Month.ToString("D2"),
                Security = cveIds?.Count > 0,
                CveCount = cveIds?.Count ?? 0,
                CveRecords = cveIds?.Count > 0 ? cveIds : null,
                SupportPhase = summary.Lifecycle?.Phase,
                Supported = summary.Lifecycle?.Supported ?? false,
                EolDate = summary.Lifecycle != null ? DateOnly.FromDateTime(summary.Lifecycle.EolDate.DateTime) : null,
                SdkVersion = sdkVersion,
                Links = patchLinks
            };

            latestPatches.Add(patchEntry);
        }

        // Find latest security month for the link
        string? latestSecurityMonthYear = null;
        string? latestSecurityMonthNumber = null;

        var allMonths = releaseHistory.Years
            .SelectMany(y => y.Value.Months.Select(m => (Year: y.Key, Month: m.Key, Data: m.Value)))
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .ToList();

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

        // Build root links
        var links = new Dictionary<string, HalLink>
        {
            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Llms}")
        };

        // Add latest links
        if (latestVersion != null)
        {
            links[LinkRelations.Latest] = new HalLink($"{Location.GitHubBaseUri}{latestVersion}/{FileNames.Index}")
            {
                Title = $"Latest release - .NET {latestVersion}"
            };
        }

        if (latestLtsVersion != null)
        {
            links[LinkRelations.LatestLts] = new HalLink($"{Location.GitHubBaseUri}{latestLtsVersion}/{FileNames.Index}")
            {
                Title = $"Latest LTS release - .NET {latestLtsVersion}"
            };
        }

        if (latestYear != null)
        {
            links[LinkRelations.LatestYear] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{latestYear}/{FileNames.Index}")
            {
                Title = $"Latest year - {latestYear}"
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
        links[LinkRelations.ReleasesIndex] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Index}")
        {
            Title = ".NET Release Index"
        };

        links[LinkRelations.TimelineIndex] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{FileNames.Index}")
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
            ReleaseKind.LlmsIndex,
            partial?.Title ?? ".NET Release Index for AI")
        {
            AiNote = partial?.AiNote ?? "ALWAYS read required_pre_read first. HAL graph—follow _links only, never construct URLs.",
            RequiredPreRead = requiredPreRead,
            Latest = latestVersion,
            LatestLts = latestLtsVersion,
            LatestYear = latestYear,
            SupportedReleases = supportedReleases,
            Links = HalHelpers.OrderLinks(links),
            Embedded = new LlmsIndexEmbedded
            {
                LatestPatches = latestPatches.Count > 0 ? latestPatches : null
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
