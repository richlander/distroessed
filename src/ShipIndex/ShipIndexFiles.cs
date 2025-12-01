using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DotnetRelease;
using DotnetRelease.Security;
using DotnetRelease.Graph;
using DotnetRelease.Summary;
using System.Linq;
using System.Globalization;
using JsonSchemaInjector;

namespace ShipIndex;

// Helper record to track patch release information
internal record PatchReleaseInfo(string PatchVersion, HashSet<string> SdkVersions);

public class ShipIndexFiles
{
    private static int _skippedFilesCount = 0;
    
    public static int SkippedFilesCount => _skippedFilesCount;
    
    public static void ResetSkippedFilesCount() => _skippedFilesCount = 0;

    public static readonly OrderedDictionary<string, FileLink> HistoryFileMappings = new()
    {
        {FileNames.Index, new FileLink(FileNames.Index, LinkTitles.HistoryIndex, LinkStyle.Prod) },
        {FileNames.Cve, new FileLink(FileNames.Cve, LinkTitles.CveInformation, LinkStyle.Prod) },
        {"cve.md", new FileLink("cve.md", LinkTitles.CveInformation, LinkStyle.Prod | LinkStyle.GitHub) },
    };

    public static readonly OrderedDictionary<string, FileLink> ReleaseFileMappings = new()
    {
        {FileNames.Index, new FileLink(FileNames.Index, LinkTitles.DotNetReleaseIndex, LinkStyle.Prod) },
        {"README.md", new FileLink("README.md", LinkTitles.DotNetReleaseNotes, LinkStyle.GitHub) },
    };

    public static async Task GenerateAsync(string inputPath, string outputPath, ReleaseHistory releaseHistory, List<MajorReleaseSummary> summaries)
    {
        var historyPath = Path.Combine(outputPath, FileNames.Directories.Timeline);

        if (!Directory.Exists(historyPath))
        {
            Directory.CreateDirectory(historyPath);
        }

        // Load glossary from centralized file (include all terms for timeline)
        var glossary = await GlossaryLoader.LoadAsync(inputPath);

        var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);

        var urlGenerator = (string relativePath, LinkStyle style) => style == LinkStyle.Prod
    ? $"{Location.GitHubBaseUri}{relativePath}"
    : $"https://github.com/dotnet/core/blob/main/release-notes/{relativePath}";


        var halLinkGenerator = new HalLinkGenerator(inputPath, urlGenerator);

        List<HistoryYearEntry> yearEntries = [];

        HashSet<string> allReleases = [];

        // Track the latest security month across all years (format: "YYYY-MM")
        string? globalLatestSecurityMonth = null;

        // Get sorted list of years for next/prev links
        var sortedYears = releaseHistory.Years.Keys.OrderBy(y => y, numericStringComparer).ToList();

        foreach (var year in releaseHistory.Years.Values)
        {
            Console.WriteLine($"Processing year: {year.Year}");
            var yearPath = Path.Combine(historyPath, year.Year);
            if (!Directory.Exists(yearPath))
            {
                Directory.CreateDirectory(yearPath);
            }

            List<HistoryMonthSummary> monthSummaries = [];
            List<HistoryMonthEntry> monthDayEntries = [];

            HashSet<string> releasesForYear = [];

            // Track all patch versions per major version for the year (for phase calculation)
            Dictionary<string, List<string>> yearPatchVersionsByMajor = new();

            // Get sorted list of months for next/prev links
            var sortedMonths = year.Months.Keys.OrderBy(m => m, numericStringComparer).ToList();

            // Calculate year index once for cross-year month navigation
            var currentYearIndex = sortedYears.IndexOf(year.Year);

            foreach (var month in year.Months.Values)
            {
                Console.WriteLine($"Processing month: {month.Month} in year: {year.Year}");
                var monthPath = Path.Combine(yearPath, month.Month);

                if (!Directory.Exists(monthPath))
                {
                    Directory.CreateDirectory(monthPath);
                }

                var monthHistoryLinks = halLinkGenerator.Generate(
                    monthPath,
                    HistoryFileMappings.Values,
                    (fileLink, key) => key == HalTerms.Self ? IndexTitles.TimelineMonthLink(year.Year, month.Month) : fileLink.Title);

                HashSet<string> monthReleases = [];
                Dictionary<string, Dictionary<string, PatchReleaseInfo>> releasesByMajor = new();

                // Process each day in the month
                foreach (var days in month.Days.Values)
                {
                    foreach (var day in days.Releases)
                    {
                        monthReleases.Add(day.MajorVersion);
                        releasesForYear.Add(day.MajorVersion);
                        allReleases.Add(day.MajorVersion);

                        // Track patch version for year-level phase calculation
                        var runtimeVersionForYear = day.Components.FirstOrDefault(c => c.Name == "Runtime")?.Version ?? day.PatchVersion;
                        if (!yearPatchVersionsByMajor.TryGetValue(day.MajorVersion, out var yearPatches))
                        {
                            yearPatches = new List<string>();
                            yearPatchVersionsByMajor[day.MajorVersion] = yearPatches;
                        }
                        if (!yearPatches.Contains(runtimeVersionForYear))
                        {
                            yearPatches.Add(runtimeVersionForYear);
                        }

                        // Group patches by major version, keyed by runtime version
                        if (!releasesByMajor.TryGetValue(day.MajorVersion, out var patches))
                        {
                            patches = new Dictionary<string, PatchReleaseInfo>();
                            releasesByMajor[day.MajorVersion] = patches;
                        }

                        // Get runtime version for this release
                        var runtimeVersion = day.Components.FirstOrDefault(c => c.Name == "Runtime")?.Version ?? day.PatchVersion;
                        
                        if (!patches.ContainsKey(runtimeVersion))
                        {
                            patches[runtimeVersion] = new PatchReleaseInfo(
                                day.PatchVersion,
                                new HashSet<string>()
                            );
                        }

                        // Collect SDK versions for this runtime release
                        foreach (var component in day.Components)
                        {
                            if (component.Name == "SDK")
                            {
                                patches[runtimeVersion].SdkVersions.Add(component.Version);
                            }
                        }
                    }
                }

                // Load CVE information for the month
                var inputMonthPath = Path.Combine(inputPath, "timeline", year.Year, month.Month);
                var cveRecords = await CveHandler.CveLoader.LoadCveRecordsFromDirectoryAsync(inputMonthPath);
                
                // Generate CVE summaries once for the month
                var cveSummariesForMonth = cveRecords != null ? CveHandler.CveTransformer.ToSummaries(cveRecords) : null;

                // Prepare month index path for links
                var monthIndexPath = Path.Combine(monthPath, FileNames.Index);
                var monthIndexRelativePath = Path.GetRelativePath(inputPath, monthIndexPath);
                var monthIndexPathValue = "/" + monthIndexRelativePath.Replace("\\", "/");

                // Create simplified month summary for year index with proper self link and CVE links
                var monthSummaryLinks = new Dictionary<string, HalLink>
                {
                    [HalTerms.Self] = new HalLink(urlGenerator(monthIndexRelativePath, LinkStyle.Prod))
                    {
                        Path = monthIndexPathValue,
                        Title = IndexTitles.TimelineMonthLink(year.Year, month.Month),
                        Type = MediaType.HalJson
                    }
                };

                // Add CVE JSON link if CVE records exist
                if (cveRecords?.Disclosures.Count > 0)
                {
                    var cveJsonRelativePath = Path.GetRelativePath(inputPath, Path.Combine(monthPath, FileNames.Cve));
                    var cveJsonPathValue = "/" + cveJsonRelativePath.Replace("\\", "/");

                    monthSummaryLinks[LinkRelations.CveJson] = new HalLink(urlGenerator(cveJsonRelativePath, LinkStyle.Prod))
                    {
                        Path = cveJsonPathValue,
                        Title = LinkTitles.CveInformation,
                        Type = MediaType.Json
                    };
                }

                // Calculate latest release for this month (highest major version)
                var monthLatestRelease = monthReleases.Max(numericStringComparer);

                var monthSummary = new HistoryMonthSummary(
                    month.Month,
                    cveSummariesForMonth?.Count > 0,
                    cveSummariesForMonth?.Count ?? 0,
                    cveSummariesForMonth?.Select(s => s.Id).ToList(),
                    monthLatestRelease,
                    [.. monthReleases],
                    monthSummaryLinks
                );
                monthSummaries.Add(monthSummary);

                // Create detailed month index with proper self link
                var monthIndexLinks = new Dictionary<string, HalLink>(monthHistoryLinks)
                {
                    [HalTerms.Self] = new HalLink(urlGenerator(monthIndexRelativePath, LinkStyle.Prod))
                    {
                        Path = monthIndexPathValue,
                        Title = IndexTitles.TimelineMonthLink(year.Year, month.Month),
                        Type = MediaType.HalJson
                    }
                };

                // Add next/prev links for month navigation (including cross-year boundaries)
                var currentMonthIndex = sortedMonths.IndexOf(month.Month);

                // Previous month link
                if (currentMonthIndex > 0)
                {
                    // Previous month in same year
                    var prevMonth = sortedMonths[currentMonthIndex - 1];
                    var prevMonthIndexPath = Path.Combine(yearPath, prevMonth, FileNames.Index);
                    var prevMonthIndexRelativePath = Path.GetRelativePath(inputPath, prevMonthIndexPath);
                    var prevMonthPathValue = "/" + prevMonthIndexRelativePath.Replace("\\", "/");
                    monthIndexLinks[HalTerms.Prev] = new HalLink(urlGenerator(prevMonthIndexRelativePath, LinkStyle.Prod))
                    {
                        Path = prevMonthPathValue,
                        Title = IndexTitles.TimelineMonthLink(year.Year, prevMonth),
                        Type = MediaType.HalJson
                    };
                }
                else if (currentYearIndex > 0)
                {
                    // First month of year - link to last month of previous year
                    var prevYear = sortedYears[currentYearIndex - 1];
                    if (releaseHistory.Years.TryGetValue(prevYear, out var prevYearData))
                    {
                        var prevYearMonths = prevYearData.Months.Keys.OrderBy(m => m, numericStringComparer).ToList();
                        if (prevYearMonths.Count > 0)
                        {
                            var lastMonthOfPrevYear = prevYearMonths.Last();
                            var prevMonthIndexPath = Path.Combine(historyPath, prevYear, lastMonthOfPrevYear, FileNames.Index);
                            var prevMonthIndexRelativePath = Path.GetRelativePath(inputPath, prevMonthIndexPath);
                            var prevMonthPathValue = "/" + prevMonthIndexRelativePath.Replace("\\", "/");
                            monthIndexLinks[HalTerms.Prev] = new HalLink(urlGenerator(prevMonthIndexRelativePath, LinkStyle.Prod))
                            {
                                Path = prevMonthPathValue,
                                Title = IndexTitles.TimelineMonthLink(prevYear, lastMonthOfPrevYear),
                                Type = MediaType.HalJson
                            };
                        }
                    }
                }

                // Next month link
                if (currentMonthIndex < sortedMonths.Count - 1)
                {
                    // Next month in same year
                    var nextMonth = sortedMonths[currentMonthIndex + 1];
                    var nextMonthIndexPath = Path.Combine(yearPath, nextMonth, FileNames.Index);
                    var nextMonthIndexRelativePath = Path.GetRelativePath(inputPath, nextMonthIndexPath);
                    var nextMonthPathValue = "/" + nextMonthIndexRelativePath.Replace("\\", "/");
                    monthIndexLinks[HalTerms.Next] = new HalLink(urlGenerator(nextMonthIndexRelativePath, LinkStyle.Prod))
                    {
                        Path = nextMonthPathValue,
                        Title = IndexTitles.TimelineMonthLink(year.Year, nextMonth),
                        Type = MediaType.HalJson
                    };
                }
                else if (currentYearIndex < sortedYears.Count - 1)
                {
                    // Last month of year - link to first month of next year
                    var nextYear = sortedYears[currentYearIndex + 1];
                    if (releaseHistory.Years.TryGetValue(nextYear, out var nextYearData))
                    {
                        var nextYearMonths = nextYearData.Months.Keys.OrderBy(m => m, numericStringComparer).ToList();
                        if (nextYearMonths.Count > 0)
                        {
                            var firstMonthOfNextYear = nextYearMonths.First();
                            var nextMonthIndexPath = Path.Combine(historyPath, nextYear, firstMonthOfNextYear, FileNames.Index);
                            var nextMonthIndexRelativePath = Path.GetRelativePath(inputPath, nextMonthIndexPath);
                            var nextMonthPathValue = "/" + nextMonthIndexRelativePath.Replace("\\", "/");
                            monthIndexLinks[HalTerms.Next] = new HalLink(urlGenerator(nextMonthIndexRelativePath, LinkStyle.Prod))
                            {
                                Path = nextMonthPathValue,
                                Title = IndexTitles.TimelineMonthLink(nextYear, firstMonthOfNextYear),
                                Type = MediaType.HalJson
                            };
                        }
                    }
                }

                // Get the latest major version for the month
                var monthLatestVersion = monthReleases.Max(numericStringComparer) ?? "unknown";

                // Get sorted major releases for the month
                var sortedMonthReleases = monthReleases
                    .OrderByDescending(v => v, numericStringComparer)
                    .ToList();

                // Latest release is the highest major version (two-part, e.g., "10.0")
                var latestReleaseForMonth = sortedMonthReleases.FirstOrDefault();

                // Create embedded releases with lifecycle based on patch versions released this month
                // Phase is determined from the patch version string, EOL from _manifest.json (via summary)
                var embeddedReleases = sortedMonthReleases
                    .Select(version =>
                    {
                        var summary = summaries.FirstOrDefault(s => s.MajorVersion == version);

                        // Determine phase from the patch versions released this month for this major version
                        var patchVersionsForMajor = releasesByMajor.TryGetValue(version, out var patches)
                            ? patches.Keys.ToList()
                            : new List<string>();

                        // Use the "best" phase among all patches (Active > GoLive > Preview)
                        // If any patch is Active (GA), the month shows Active
                        // Otherwise if any patch is GoLive (RC), show GoLive
                        // Otherwise Preview
                        var bestPhase = patchVersionsForMajor
                            .Select(ReleaseStability.DeterminePhaseFromVersion)
                            .OrderBy(p => p) // Active(2) < GoLive(1) < Preview(0) - but enum order is Preview=0, GoLive=1, Active=2
                            .LastOrDefault(); // Take highest (Active if present)

                        // If no patches found, fall back to preview
                        if (patchVersionsForMajor.Count == 0)
                        {
                            bestPhase = SupportPhase.Preview;
                        }

                        // Create lifecycle with phase from version, other data from summary
                        Lifecycle? lifecycle = null;
                        if (summary?.Lifecycle != null)
                        {
                            lifecycle = new Lifecycle(
                                summary.Lifecycle.ReleaseType,
                                bestPhase,
                                summary.Lifecycle.GaDate,
                                summary.Lifecycle.EolDate)
                            {
                                Supported = ReleaseStability.IsSupportedPhase(bestPhase) && DateTimeOffset.UtcNow < summary.Lifecycle.EolDate
                            };
                        }

                        // Build links for this release entry - HAL+JSON first, then JSON, then Markdown
                        var releaseLinks = new Dictionary<string, HalLink>
                        {
                            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{version}/{FileNames.Index}")
                            {
                                Path = $"/{version}/{FileNames.Index}",
                                Title = $".NET {version}",
                                Type = MediaType.HalJson
                            }
                        };

                        // Get patches for this version
                        string? latestPatch = null;
                        IList<string>? runtimePatches = null;
                        IList<string>? sdkPatches = null;

                        if (releasesByMajor.TryGetValue(version, out var patchesForVersion))
                        {
                            runtimePatches = patchesForVersion.Keys
                                .OrderByDescending(v => v, numericStringComparer)
                                .ToList();
                            latestPatch = runtimePatches.FirstOrDefault();

                            var sdks = patchesForVersion.Values
                                .SelectMany(p => p.SdkVersions)
                                .Distinct()
                                .OrderByDescending(v => v, numericStringComparer)
                                .ToList();

                            sdkPatches = sdks.Count > 0 ? sdks : null;
                        }

                        // Add release-patch link (HAL+JSON)
                        if (latestPatch != null)
                        {
                            // Determine the correct path - previews/RCs are in a different structure
                            string? patchIndexPath = null;

                            if (latestPatch.Contains("-preview.") || latestPatch.Contains("-rc."))
                            {
                                // Extract preview/rc number: "10.0.0-preview.1.25080.5" -> "preview1" or "10.0.0-rc.1.xxx" -> "rc1"
                                var dashIndex = latestPatch.IndexOf('-');
                                if (dashIndex > 0)
                                {
                                    var suffix = latestPatch.Substring(dashIndex + 1); // "preview.1.25080.5" or "rc.1.xxx"
                                    var parts = suffix.Split('.');
                                    if (parts.Length >= 2)
                                    {
                                        var previewOrRc = parts[0]; // "preview" or "rc"
                                        var number = parts[1];       // "1", "2", etc.
                                        var subdir = $"{previewOrRc}{number}"; // "preview1" or "rc1"
                                        patchIndexPath = $"{version}/preview/{subdir}/{FileNames.Index}";
                                    }
                                }
                            }
                            else
                            {
                                // GA release - standard path
                                patchIndexPath = $"{version}/{latestPatch}/{FileNames.Index}";
                            }

                            if (patchIndexPath != null)
                            {
                                releaseLinks["release-patch"] = new HalLink($"{Location.GitHubBaseUri}{patchIndexPath}")
                                {
                                    Path = $"/{patchIndexPath}",
                                    Title = $".NET {latestPatch}",
                                    Type = MediaType.HalJson
                                };
                            }
                        }

                        // Add latest-sdk link (HAL+JSON) - only if the index.json exists
                        var sdkIndexPath = $"{version}/{FileNames.Directories.Sdk}/{FileNames.Index}";
                        var fullSdkIndexPath = Path.Combine(inputPath, sdkIndexPath);
                        if (File.Exists(fullSdkIndexPath))
                        {
                            releaseLinks[LinkRelations.LatestSdk] = new HalLink($"{Location.GitHubBaseUri}{sdkIndexPath}")
                            {
                                Path = $"/{sdkIndexPath}",
                                Title = $".NET SDK {version} Release Information",
                                Type = MediaType.HalJson
                            };
                        }

                        // Filter CVE IDs for this major version
                        IList<string>? majorVersionCveIds = null;
                        if (cveSummariesForMonth != null)
                        {
                            var filteredCves = cveSummariesForMonth
                                .Where(cve => cve.AffectedReleases?.Contains(version) == true)
                                .ToList();

                            if (filteredCves.Count > 0)
                            {
                                majorVersionCveIds = filteredCves.Select(cve => cve.Id).ToList();
                            }
                        }

                        // Add CVE links (JSON then Markdown) - only if there are CVEs for this version
                        if (majorVersionCveIds != null)
                        {
                            var cveJsonPath = $"{FileNames.Directories.Timeline}/{year.Year}/{month.Month}/{FileNames.Cve}";
                            releaseLinks[LinkRelations.CveJson] = new HalLink($"{Location.GitHubBaseUri}{cveJsonPath}")
                            {
                                Path = $"/{cveJsonPath}",
                                Title = "CVE Information",
                                Type = MediaType.Json
                            };

                            var cveMdPath = $"{FileNames.Directories.Timeline}/{year.Year}/{month.Month}/cve.md";
                            releaseLinks["cve-markdown"] = new HalLink($"{Location.GitHubBaseUri}{cveMdPath}")
                            {
                                Path = $"/{cveMdPath}",
                                Title = "CVE Information",
                                Type = MediaType.Markdown
                            };
                            releaseLinks["cve-markdown-rendered"] = new HalLink($"https://github.com/dotnet/core/blob/main/release-notes/{cveMdPath}")
                            {
                                Path = $"/{cveMdPath}",
                                Title = "CVE Information (Rendered)",
                                Type = MediaType.Markdown
                            };
                        }

                        return new MajorReleaseVersionIndexEntry(version)
                        {
                            ReleaseType = lifecycle?.ReleaseType,
                            Phase = lifecycle?.Phase,
                            Supported = lifecycle?.Supported,
                            Security = majorVersionCveIds?.Count > 0,
                            CveCount = majorVersionCveIds?.Count ?? 0,
                            GaDate = lifecycle?.GaDate,
                            EolDate = lifecycle?.EolDate,
                            CveRecords = majorVersionCveIds,
                            RuntimePatches = runtimePatches,
                            SdkPatches = sdkPatches,
                            Links = HalHelpers.OrderLinks(releaseLinks)
                        };
                    })
                    .ToList();

                // Extract CVE IDs from disclosures for root-level quick enumeration
                var monthCveIds = cveSummariesForMonth?.Select(d => d.Id).ToList();

                var monthIndex = new HistoryMonthIndex(
                    HistoryKind.MonthIndex,
                    IndexTitles.TimelineMonthTitle(year.Year, month.Month),
                    IndexTitles.TimelineMonthIndexDescription(year.Year, month.Month, monthLatestVersion),
                    year.Year,
                    month.Month,
                    cveSummariesForMonth?.Count > 0)
                {
                    CveCount = monthCveIds?.Count > 0 ? monthCveIds.Count : null,
                    CveRecords = monthCveIds?.Count > 0 ? monthCveIds : null,
                    LatestRelease = latestReleaseForMonth,
                    Releases = sortedMonthReleases,
                    Links = HalHelpers.OrderLinks(monthIndexLinks),
                    Embedded = new HistoryMonthIndexEmbedded
                    {
                        Releases = embeddedReleases,
                        Disclosures = cveSummariesForMonth
                    },
                    Metadata = new GenerationMetadata("1.0", DateTimeOffset.UtcNow, "ShipIndex")
                };

                // Serialize to string first to add schema reference
                var monthIndexJson = JsonSerializer.Serialize(
                    monthIndex,
                    HistoryYearIndexSerializerContext.Default.HistoryMonthIndex);

                // Add schema reference
                var monthSchemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.TimelineIndex}";
                var updatedMonthIndexJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(monthIndexJson, monthSchemaUri);

                // Write monthly index file
                var currentMonthIndexPath = Path.Combine(monthPath, FileNames.Index);
                var finalMonthIndexJson = (updatedMonthIndexJson ?? monthIndexJson) + '\n';
                
                if (HalJsonComparer.ShouldWriteFile(currentMonthIndexPath, finalMonthIndexJson))
                {
                    using Stream monthStream = File.Create(currentMonthIndexPath);
                    using var monthWriter = new StreamWriter(monthStream);
                    await monthWriter.WriteAsync(finalMonthIndexJson);
                }
                else
                {
                    _skippedFilesCount++;
                }
            }

            // Generate the root links for the year index
            var yearHalLinks = halLinkGenerator.Generate(
                yearPath,
                HistoryFileMappings.Values,
                (fileLink, key) => key == HalTerms.Self ? IndexTitles.TimelineYearLink(year.Year) : fileLink.Title);

            // Add self link for year index (generated file may not exist yet)
            var yearIndexRelativePath = Path.GetRelativePath(inputPath, Path.Combine(yearPath, FileNames.Index));
            var yearIndexPathValue = "/" + yearIndexRelativePath.Replace("\\", "/");
            yearHalLinks[HalTerms.Self] = new HalLink(urlGenerator(yearIndexRelativePath, LinkStyle.Prod))
            {
                Path = yearIndexPathValue,
                Title = IndexTitles.TimelineYearLink(year.Year),
                Type = MediaType.HalJson
            };

            // Add next/prev links for year navigation (currentYearIndex already calculated above)
            if (currentYearIndex > 0)
            {
                var prevYear = sortedYears[currentYearIndex - 1];
                var prevYearPath = Path.Combine(historyPath, prevYear);
                var prevYearIndexPath = Path.Combine(prevYearPath, FileNames.Index);
                var prevYearIndexRelativePath = Path.GetRelativePath(inputPath, prevYearIndexPath);
                var prevYearPathValue = "/" + prevYearIndexRelativePath.Replace("\\", "/");
                yearHalLinks[HalTerms.Prev] = new HalLink(urlGenerator(prevYearIndexRelativePath, LinkStyle.Prod))
                {
                    Path = prevYearPathValue,
                    Title = IndexTitles.TimelineYearLink(prevYear),
                    Type = MediaType.HalJson
                };
            }
            if (currentYearIndex < sortedYears.Count - 1)
            {
                var nextYear = sortedYears[currentYearIndex + 1];
                var nextYearPath = Path.Combine(historyPath, nextYear);
                var nextYearIndexPath = Path.Combine(nextYearPath, FileNames.Index);
                var nextYearIndexRelativePath = Path.GetRelativePath(inputPath, nextYearIndexPath);
                var nextYearPathValue = "/" + nextYearIndexRelativePath.Replace("\\", "/");
                yearHalLinks[HalTerms.Next] = new HalLink(urlGenerator(nextYearIndexRelativePath, LinkStyle.Prod))
                {
                    Path = nextYearPathValue,
                    Title = IndexTitles.TimelineYearLink(nextYear),
                    Type = MediaType.HalJson
                };
            }

            // Get the latest major version for the year
            var yearLatestVersion = releasesForYear.Max(numericStringComparer) ?? "unknown";

            // Calculate latest month for this year (months are ordered latest first)
            var latestMonth = monthSummaries.FirstOrDefault()?.Month;

            // Add latest-month link if available
            if (latestMonth != null)
            {
                var latestMonthPath = Path.Combine(yearPath, latestMonth, FileNames.Index);
                var latestMonthRelativePath = Path.GetRelativePath(inputPath, latestMonthPath);
                var latestMonthPathValue = "/" + latestMonthRelativePath.Replace("\\", "/");
                yearHalLinks[LinkRelations.LatestMonth] = new HalLink(urlGenerator(latestMonthRelativePath, LinkStyle.Prod))
                {
                    Path = latestMonthPathValue,
                    Title = $"Latest month ({IndexTitles.TimelineMonthLink(year.Year, latestMonth)})",
                    Type = MediaType.HalJson
                };
            }

            // Calculate latest security month for this year
            var latestSecurityMonth = monthSummaries.FirstOrDefault(m => m.Security)?.Month;

            // Update global latest security month tracker (comparing YYYY-MM strings)
            if (latestSecurityMonth != null)
            {
                var yearMonthString = $"{year.Year}-{latestSecurityMonth}";
                if (globalLatestSecurityMonth == null ||
                    string.Compare(yearMonthString, globalLatestSecurityMonth, StringComparison.Ordinal) > 0)
                {
                    globalLatestSecurityMonth = yearMonthString;
                }
            }

            // Add latest-security-month link if available
            if (latestSecurityMonth != null)
            {
                var latestSecurityMonthPath = Path.Combine(yearPath, latestSecurityMonth, FileNames.Index);
                var latestSecurityMonthRelativePath = Path.GetRelativePath(inputPath, latestSecurityMonthPath);
                var latestSecurityMonthPathValue = "/" + latestSecurityMonthRelativePath.Replace("\\", "/");
                yearHalLinks[LinkRelations.LatestSecurityMonth] = new HalLink(urlGenerator(latestSecurityMonthRelativePath, LinkStyle.Prod))
                {
                    Path = latestSecurityMonthPathValue,
                    Title = $"Latest security month ({IndexTitles.TimelineMonthLink(year.Year, latestSecurityMonth)})",
                    Type = MediaType.HalJson
                };
            }

            // Calculate latest release and sorted releases for the year
            var sortedReleasesForYear = releasesForYear
                .OrderByDescending(v => v, numericStringComparer)
                .ToList();
            // Latest release is the highest major version (two-part, e.g., "10.0")
            var latestReleaseForYear = sortedReleasesForYear.FirstOrDefault();

            // Add latest-release link if available
            if (latestReleaseForYear != null)
            {
                var latestReleaseIndexPath = $"{latestReleaseForYear}/{FileNames.Index}";
                yearHalLinks[LinkRelations.LatestRelease] = new HalLink($"{Location.GitHubBaseUri}{latestReleaseIndexPath}")
                {
                    Path = $"/{latestReleaseIndexPath}",
                    Title = $"Latest release (.NET {latestReleaseForYear})",
                    Type = MediaType.HalJson
                };
            }

            // Create the year index (e.g., release-notes/2025/index.json)
            var yearHistory = new HistoryYearIndex(
                HistoryKind.YearIndex,
                IndexTitles.TimelineYearTitle(year.Year),
                IndexTitles.TimelineYearIndexDescription(year.Year, yearLatestVersion),
                year.Year)
            {
                LatestMonth = latestMonth,
                LatestSecurityMonth = latestSecurityMonth,
                LatestRelease = latestReleaseForYear,
                Releases = sortedReleasesForYear.Count > 0 ? sortedReleasesForYear : null,
                Links = HalHelpers.OrderLinks(yearHalLinks),
                Metadata = new GenerationMetadata("1.0", DateTimeOffset.UtcNow, "ShipIndex")
            };

            // Create embedded releases with lifecycle based on patch versions released this year
            // Phase is determined from the patch version strings, EOL from _manifest.json (via summary)
            var releaseEntries = releasesForYear
                .OrderByDescending(v => v, numericStringComparer)
                .Select(version =>
                {
                    var summary = summaries.FirstOrDefault(s => s.MajorVersion == version);

                    // Determine phase from the patch versions released this year for this major version
                    var patchVersionsForMajor = yearPatchVersionsByMajor.TryGetValue(version, out var yearPatches)
                        ? yearPatches
                        : new List<string>();

                    // Use the "best" phase among all patches (Active > GoLive > Preview)
                    var bestPhase = patchVersionsForMajor
                        .Select(ReleaseStability.DeterminePhaseFromVersion)
                        .OrderBy(p => p)
                        .LastOrDefault();

                    // If no patches found, fall back to preview
                    if (patchVersionsForMajor.Count == 0)
                    {
                        bestPhase = SupportPhase.Preview;
                    }

                    // Create lifecycle with phase from version, other data from summary
                    Lifecycle? lifecycle = null;
                    if (summary?.Lifecycle != null)
                    {
                        lifecycle = new Lifecycle(
                            summary.Lifecycle.ReleaseType,
                            bestPhase,
                            summary.Lifecycle.GaDate,
                            summary.Lifecycle.EolDate)
                        {
                            Supported = ReleaseStability.IsSupportedPhase(bestPhase) && DateTimeOffset.UtcNow < summary.Lifecycle.EolDate
                        };
                    }

                    // Build links dictionary starting with self
                    var links = new Dictionary<string, HalLink>
                    {
                        [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{version}/{FileNames.Index}")
                        {
                            Path = $"/{version}/{FileNames.Index}",
                            Title = $".NET {version}",
                            Type = MediaType.HalJson
                        }
                    };

                    // Find the latest patch for this release within this year
                    var latestPatchForYear = summary?.PatchReleases
                        .Where(p => p.ReleaseDate.Year.ToString() == year.Year)
                        .OrderByDescending(p => p.ReleaseDate)
                        .FirstOrDefault();

                    if (latestPatchForYear != null)
                    {
                        // Add latest-patch link
                        var latestPatchPath = $"{version}/{latestPatchForYear.PatchVersion}/{FileNames.Index}";
                        links[LinkRelations.LatestPatch] = new HalLink($"{Location.GitHubBaseUri}{latestPatchPath}")
                        {
                            Path = $"/{latestPatchPath}",
                            Title = $"Latest patch ({latestPatchForYear.PatchVersion})",
                            Type = MediaType.HalJson
                        };

                        // Add latest-month link based on the latest patch's release date
                        var patchYear = latestPatchForYear.ReleaseDate.Year.ToString("D4");
                        var patchMonth = latestPatchForYear.ReleaseDate.Month.ToString("D2");
                        var latestMonthPath = $"{FileNames.Directories.Timeline}/{patchYear}/{patchMonth}/{FileNames.Index}";
                        links[LinkRelations.LatestMonth] = new HalLink($"{Location.GitHubBaseUri}{latestMonthPath}")
                        {
                            Path = $"/{latestMonthPath}",
                            Title = $"Latest month ({patchYear}-{patchMonth})",
                            Type = MediaType.HalJson
                        };
                    }

                    return new MajorReleaseVersionIndexEntry(version)
                    {
                        ReleaseType = lifecycle?.ReleaseType,
                        Phase = lifecycle?.Phase,
                        Supported = lifecycle?.Supported,
                        GaDate = lifecycle?.GaDate,
                        EolDate = lifecycle?.EolDate,
                        Links = HalHelpers.OrderLinks(links)
                    };
                })
                .ToList();

            yearHistory.Embedded = new HistoryYearIndexEmbedded
            {
                Months = monthSummaries,
                Releases = releaseEntries
            };

            // Serialize to string first to add schema reference
            var yearIndexJson = JsonSerializer.Serialize(
                yearHistory,
                HistoryYearIndexSerializerContext.Default.HistoryYearIndex);

            // Add schema reference
            var yearSchemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.TimelineIndex}";
            var updatedYearIndexJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(yearIndexJson, yearSchemaUri);

            var yearIndexPath = Path.Combine(yearPath, FileNames.Index);
            var finalYearIndexJson = (updatedYearIndexJson ?? yearIndexJson) + '\n';
            
            if (HalJsonComparer.ShouldWriteFile(yearIndexPath, finalYearIndexJson))
            {
                using Stream yearStream = File.Create(yearIndexPath);
                using var yearWriter = new StreamWriter(yearStream);
                await yearWriter.WriteAsync(finalYearIndexJson);
            }
            else
            {
                _skippedFilesCount++;
            }

            // for the overall index

            var overallYearHalLinks = halLinkGenerator.Generate(
                yearPath,
                HistoryFileMappings.Values,
                (fileLink, key) => key == HalTerms.Self ? IndexTitles.TimelineYearLink(year.Year) : fileLink.Title);

            // Add latest-month link to year entry for symmetry with release entries
            if (latestMonth != null)
            {
                var latestMonthPath = Path.Combine(yearPath, latestMonth, FileNames.Index);
                var latestMonthRelativePath = Path.GetRelativePath(inputPath, latestMonthPath);
                var latestMonthPathValue = "/" + latestMonthRelativePath.Replace("\\", "/");
                overallYearHalLinks[LinkRelations.LatestMonth] = new HalLink(urlGenerator(latestMonthRelativePath, LinkStyle.Prod))
                {
                    Path = latestMonthPathValue,
                    Title = $"Latest month ({IndexTitles.TimelineMonthLink(year.Year, latestMonth)})",
                    Type = MediaType.HalJson
                };
            }

            yearEntries.Add(new HistoryYearEntry(
                year.Year,
                IndexTitles.TimelineYearDescription(year.Year))
            {
                Releases = [.. releasesForYear],
                Links = HalHelpers.OrderLinks(overallYearHalLinks)
            }
            );
        }

        var fullIndexLinks = halLinkGenerator.Generate(
            historyPath,
            HistoryFileMappings.Values,
            (fileLink, key) => key == HalTerms.Self ? IndexTitles.TimelineIndexLink : fileLink.Title);

        // Calculate latest year
        var latestYear = sortedYears.LastOrDefault();

        // Find latest stable release and latest LTS release for cross-references
        // Uses shared ReleaseStability methods to ensure consistent logic across tools
        var releaseData = summaries.Select(s => (s.MajorVersion, (Lifecycle?)s.Lifecycle));
        var latestVersion = ReleaseStability.FindLatestVersion(releaseData, numericStringComparer);
        var latestLtsVersion = ReleaseStability.FindLatestLtsVersion(releaseData, numericStringComparer);
        var latestRelease = latestVersion != null ? summaries.First(s => s.MajorVersion == latestVersion) : null;
        var latestLtsRelease = latestLtsVersion != null ? summaries.First(s => s.MajorVersion == latestLtsVersion) : null;

        // Add releases-index link pointing back to root index.json
        fullIndexLinks[LinkRelations.ReleasesIndex] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Index}")
        {
            Path = $"/{FileNames.Index}",
            Title = IndexTitles.VersionIndexTitle,
            Type = MediaType.HalJson
        };

        // Add cross-reference links to latest versions (from releases-index)
        if (latestRelease != null)
        {
            fullIndexLinks[LinkRelations.Latest] = new HalLink($"{Location.GitHubBaseUri}{latestRelease.MajorVersion}/{FileNames.Index}")
            {
                Path = $"/{latestRelease.MajorVersion}/{FileNames.Index}",
                Title = $"Latest .NET release (.NET {latestRelease.MajorVersion})",
                Type = MediaType.HalJson
            };
        }

        if (latestLtsRelease != null)
        {
            fullIndexLinks[LinkRelations.LatestLts] = new HalLink($"{Location.GitHubBaseUri}{latestLtsRelease.MajorVersion}/{FileNames.Index}")
            {
                Path = $"/{latestLtsRelease.MajorVersion}/{FileNames.Index}",
                Title = $"Latest LTS release (.NET {latestLtsRelease.MajorVersion})",
                Type = MediaType.HalJson
            };
        }

        // Add latest-year link
        if (latestYear != null)
        {
            fullIndexLinks[LinkRelations.LatestYear] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{latestYear}/{FileNames.Index}")
            {
                Path = $"/{FileNames.Directories.Timeline}/{latestYear}/{FileNames.Index}",
                Title = $"Latest year ({latestYear})",
                Type = MediaType.HalJson
            };
        }

        // Add latest-security-month link (global across all years)
        if (globalLatestSecurityMonth != null)
        {
            // Parse "YYYY-MM" format
            var parts = globalLatestSecurityMonth.Split('-');
            var secYear = parts[0];
            var secMonth = parts[1];
            fullIndexLinks[LinkRelations.LatestSecurityMonth] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{secYear}/{secMonth}/{FileNames.Index}")
            {
                Path = $"/{FileNames.Directories.Timeline}/{secYear}/{secMonth}/{FileNames.Index}",
                Title = $"Latest security month ({globalLatestSecurityMonth})",
                Type = MediaType.HalJson
            };
        }

        // Get the latest major version for the root history index
        var rootLatestVersion = allReleases.Max(numericStringComparer) ?? "unknown";

        // Create the history index
        var historyIndex = new ReleaseHistoryIndex(
            HistoryKind.TimelineIndex,
            IndexTitles.TimelineIndexTitle,
            IndexTitles.TimelineIndexDescription(rootLatestVersion))
        {
            LatestYear = latestYear,
            Latest = latestRelease?.MajorVersion,
            LatestLts = latestLtsRelease?.MajorVersion,
            Links = HalHelpers.OrderLinks(fullIndexLinks),
            Glossary = glossary,
            Embedded = new ReleaseHistoryIndexEmbedded
            {
                Years = [.. yearEntries.OrderByDescending(e => e.Year, StringComparer.OrdinalIgnoreCase)]
            },
            Metadata = new GenerationMetadata("1.0", DateTimeOffset.UtcNow, "ShipIndex")
        };

        // Serialize to string first to add schema reference
        var historyIndexJson = JsonSerializer.Serialize(
            historyIndex,
            ReleaseHistoryIndexSerializerContext.Default.ReleaseHistoryIndex);

        // Add schema reference
        var historySchemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.TimelineIndex}";
        var updatedHistoryIndexJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(historyIndexJson, historySchemaUri);

        var historyIndexPath = Path.Combine(historyPath, FileNames.Index);
        var finalHistoryIndexJson = (updatedHistoryIndexJson ?? historyIndexJson) + '\n';
        
        if (HalJsonComparer.ShouldWriteFile(historyIndexPath, finalHistoryIndexJson))
        {
            using var historyStream = File.Create(historyIndexPath);
            using var historyWriter = new StreamWriter(historyStream);
            await historyWriter.WriteAsync(finalHistoryIndexJson);
        }
        else
        {
            _skippedFilesCount++;
        }
    }
}
