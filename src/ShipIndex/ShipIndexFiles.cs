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
    // Links for timeline root index (timeline/index.json)
    public static readonly OrderedDictionary<string, FileLink> TimelineRootFileMappings = new()
    {
        {FileNames.Index, new FileLink(FileNames.Index, LinkTitles.HistoryIndex, LinkStyle.Prod) },
    };

    // Links for timeline year index (timeline/YYYY/index.json)
    public static readonly OrderedDictionary<string, FileLink> TimelineYearFileMappings = new()
    {
        {FileNames.Index, new FileLink(FileNames.Index, LinkTitles.HistoryIndex, LinkStyle.Prod) },
    };

    // Links for timeline month index (timeline/YYYY/MM/index.json) - JSON only, no markdown
    public static readonly OrderedDictionary<string, FileLink> HistoryFileMappings = new()
    {
        {FileNames.Index, new FileLink(FileNames.Index, LinkTitles.HistoryIndex, LinkStyle.Prod) },
        {FileNames.Cve, new FileLink(FileNames.Cve, LinkTitles.CveRecordsJson, LinkStyle.Prod) },
    };

    // Links for timeline month manifest (timeline/YYYY/MM/manifest.json) - markdown/documentation links
    public static readonly OrderedDictionary<string, FileLink> HistoryManifestFileMappings = new()
    {
        {"cve.md", new FileLink("cve.md", LinkTitles.CveMarkdown, LinkStyle.Prod | LinkStyle.GitHub) },
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

        var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);

        var urlGenerator = (string relativePath, LinkStyle style) => style == LinkStyle.Prod
    ? $"{Location.GitHubBaseUri}{relativePath}"
    : $"https://github.com/dotnet/core/blob/main/release-notes/{relativePath}";


        var halLinkGenerator = new HalLinkGenerator(inputPath, urlGenerator);

        List<HistoryYearEntry> yearEntries = [];

        HashSet<string> allReleases = [];

        // Track the latest security month across all years (format: "YYYY-MM")
        string? globalLatestSecurityMonth = null;

        // Track previous security month for prev-security links (year, month)
        // This is updated as we process months chronologically
        (string Year, string Month)? previousSecurityMonth = null;
        DateTimeOffset? previousSecurityMonthDate = null;

        // Track previous month date for prev-month-date property
        DateTimeOffset? previousMonthDate = null;

        // Get sorted list of years for next/prev links
        var sortedYears = releaseHistory.Years.Keys.OrderBy(y => y, numericStringComparer).ToList();

        // Iterate years in chronological order for proper prev-security tracking
        foreach (var yearKey in sortedYears)
        {
            var year = releaseHistory.Years[yearKey];
            Console.WriteLine($"Processing year: {year.Year}");
            var yearPath = Path.Combine(historyPath, year.Year);
            if (!Directory.Exists(yearPath))
            {
                Directory.CreateDirectory(yearPath);
            }

            List<HistoryMonthSummary> monthSummaries = [];
            List<HistoryMonthEntry> monthDayEntries = [];

            HashSet<string> releasesForYear = [];

            // Get sorted list of months for next/prev links
            var sortedMonths = year.Months.Keys.OrderBy(m => m, numericStringComparer).ToList();

            // Calculate year index once for cross-year month navigation
            var currentYearIndex = sortedYears.IndexOf(year.Year);

            // Iterate months in chronological order for proper prev-security tracking
            foreach (var monthKey in sortedMonths)
            {
                var month = year.Months[monthKey];
                Console.WriteLine($"Processing month: {month.Month} in year: {year.Year}");
                var monthPath = Path.Combine(yearPath, month.Month);

                if (!Directory.Exists(monthPath))
                {
                    Directory.CreateDirectory(monthPath);
                }

                var monthHistoryLinks = halLinkGenerator.Generate(
                    monthPath,
                    HistoryFileMappings.Values,
                    (fileLink, key) => key switch
                    {
                        HalTerms.Self => IndexTitles.TimelineMonthLink(year.Year, month.Month),
                        LinkRelations.CveJson => $"CVE records - {IndexTitles.FormatMonthYear(year.Year, month.Month)}",
                        _ => fileLink.Title
                    });

                HashSet<string> monthReleases = [];
                Dictionary<string, Dictionary<string, PatchReleaseInfo>> releasesByMajor = new();
                DateTimeOffset? monthReleaseDate = null;

                // Process each day in the month
                foreach (var days in month.Days.Values)
                {
                    foreach (var day in days.Releases)
                    {
                        monthReleases.Add(day.MajorVersion);
                        releasesForYear.Add(day.MajorVersion);
                        allReleases.Add(day.MajorVersion);

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

                        // Track release date for the month (use the latest date from any patch)
                        var patchSummaryForDate = summaries
                            .FirstOrDefault(s => s.MajorVersion == day.MajorVersion)
                            ?.PatchReleases.FirstOrDefault(p => p.PatchVersion == day.PatchVersion);
                        if (patchSummaryForDate != null)
                        {
                            var patchDate = new DateTimeOffset(patchSummaryForDate.ReleaseDate, TimeOnly.MinValue, TimeSpan.Zero);
                            if (monthReleaseDate == null || patchDate > monthReleaseDate)
                            {
                                monthReleaseDate = patchDate;
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

                // Create simplified month summary for year index with proper self link (href only) and CVE links
                var monthSummaryLinks = new Dictionary<string, HalLink>
                {
                    [HalTerms.Self] = new HalLink(urlGenerator(monthIndexRelativePath, LinkStyle.Prod))
                };

                // Add CVE JSON link if CVE records exist
                if (cveRecords?.Disclosures.Count > 0)
                {
                    var cveJsonRelativePath = Path.GetRelativePath(inputPath, Path.Combine(monthPath, FileNames.Cve));
                    var cveJsonPathValue = "/" + cveJsonRelativePath.Replace("\\", "/");

                    monthSummaryLinks[LinkRelations.CveJson] = new HalLink(urlGenerator(cveJsonRelativePath, LinkStyle.Prod))
                    {
                        Type = MediaType.Json
                    };
                }

                var monthSummary = new HistoryMonthSummary(
                    month.Month,
                    cveSummariesForMonth?.Count > 0,
                    monthSummaryLinks
                );
                monthSummaries.Add(monthSummary);

                // Create detailed month index with proper self link - href only
                var monthIndexLinks = new Dictionary<string, HalLink>(monthHistoryLinks)
                {
                    [HalTerms.Self] = new HalLink(urlGenerator(monthIndexRelativePath, LinkStyle.Prod))
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
                    monthIndexLinks[LinkRelations.PrevMonth] = new HalLink(urlGenerator(prevMonthIndexRelativePath, LinkStyle.Prod))
                    {
                        Title = $"Previous month - {IndexTitles.FormatMonthYear(year.Year, prevMonth)}",
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
                            monthIndexLinks[LinkRelations.PrevMonth] = new HalLink(urlGenerator(prevMonthIndexRelativePath, LinkStyle.Prod))
                            {
                                Title = $"Previous month - {IndexTitles.FormatMonthYear(prevYear, lastMonthOfPrevYear)}",
                            };
                        }
                    }
                }

                // NOTE: No "next" links - month indexes are immutable once created.
                // Navigation pattern: start from latest-month and walk backwards via "prev" links.

                // Add prev-security link for security month navigation
                // This links to the previous month that had security releases (CVEs)
                if (previousSecurityMonth != null)
                {
                    var prevSecurityMonthIndexPath = Path.Combine(historyPath, previousSecurityMonth.Value.Year, previousSecurityMonth.Value.Month, FileNames.Index);
                    var prevSecurityMonthIndexRelativePath = Path.GetRelativePath(inputPath, prevSecurityMonthIndexPath);
                    monthIndexLinks[LinkRelations.PrevSecurityMonth] = new HalLink(urlGenerator(prevSecurityMonthIndexRelativePath, LinkStyle.Prod))
                    {
                        Title = $"Previous security month - {IndexTitles.FormatMonthYear(previousSecurityMonth.Value.Year, previousSecurityMonth.Value.Month)}",
                    };
                }

                // Add timeline-index link (grandparent)
                monthIndexLinks[LinkRelations.Timeline] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{FileNames.Index}")
                {
                    Title = IndexTitles.TimelineIndexLink,
                };

                // Add year-index link (parent)
                monthIndexLinks[LinkRelations.Year] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{year.Year}/{FileNames.Index}")
                {
                    Title = IndexTitles.TimelineYearLink(year.Year),
                };

                // Add manifest link for documentation/markdown resources
                monthIndexLinks[LinkRelations.Manifest] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{year.Year}/{month.Month}/{FileNames.Manifest}")
                {
                    Title = $"Manifest - {IndexTitles.FormatMonthYear(year.Year, month.Month)}",
                };

                // Get the latest major version for the month
                var monthLatestVersion = monthReleases.Max(numericStringComparer) ?? "unknown";

                // Get sorted major releases for the month
                var sortedMonthReleases = monthReleases
                    .OrderByDescending(v => v, numericStringComparer)
                    .ToList();

                // Create embedded releases - patch-centric (symmetric with major version index structure)
                // Flatten all patches from all major versions released this month
                // Note: Timeline represents historical releases, so we include ALL patches (including previews)
                // even if the major version has since reached GA. This preserves release history.
                var embeddedReleases = sortedMonthReleases
                    .SelectMany(majorVersion =>
                    {
                        if (!releasesByMajor.TryGetValue(majorVersion, out var patches))
                            return Enumerable.Empty<PatchReleaseVersionIndexEntry>();

                        var summary = summaries.FirstOrDefault(s => s.MajorVersion == majorVersion);

                        return patches.Keys.Select(patchVersion =>
                        {
                            var patchInfo = patches[patchVersion];
                            var phase = ReleaseStability.DeterminePhaseFromVersion(patchVersion);

                            // Filter CVE IDs for this major version
                            IReadOnlyList<string>? patchCveIds = null;
                            if (cveSummariesForMonth != null)
                            {
                                var filteredCves = cveSummariesForMonth
                                    .Where(cve => cve.AffectedReleases?.Contains(majorVersion) == true)
                                    .Select(cve => cve.Id)
                                    .ToList();
                                patchCveIds = filteredCves.Count > 0 ? filteredCves : null;
                            }

                            // Get SDK versions for this specific patch
                            var sdkVersions = patchInfo.SdkVersions.Count > 0
                                ? patchInfo.SdkVersions.OrderByDescending(v => v, numericStringComparer).ToList()
                                : null;

                            // Find the patch summary to get PatchDirPath for the self link
                            var patchSummaryForLinks = summary?.PatchReleases.FirstOrDefault(p => p.PatchVersion == patchVersion);

                            // Build links - self points to patch detail index
                            // Use PatchDirPath if available (handles preview/rc paths correctly)
                            var patchIndexPath = patchSummaryForLinks?.PatchDirPath != null
                                ? $"{patchSummaryForLinks.PatchDirPath}/{FileNames.Index}"
                                : $"{majorVersion}/{patchVersion}/{FileNames.Index}";

                            var patchLinks = new Dictionary<string, HalLink>
                            {
                                [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{patchIndexPath}")
                            };

                            // Get release date from the summary's patch releases if available
                            // Fall back to month release date if specific patch date not found
                            var patchSummary = summary?.PatchReleases.FirstOrDefault(p => p.PatchVersion == patchVersion);
                            var releaseDate = patchSummary != null
                                ? new DateTimeOffset(patchSummary.ReleaseDate, TimeOnly.MinValue, TimeSpan.Zero)
                                : monthReleaseDate ?? new DateTimeOffset(int.Parse(year.Year), int.Parse(month.Month), 1, 0, 0, 0, TimeSpan.Zero);

                            // Note: CVE IDs (cve_records) are intentionally omitted from patch entries.
                            // CVEs are a timeline concept - use disclosures[] at month level or cve-json link.
                            return new PatchReleaseVersionIndexEntry(
                                patchVersion,
                                releaseDate,
                                year.Year,
                                month.Month,
                                patchCveIds?.Count > 0,
                                phase,
                                HalHelpers.OrderLinks(patchLinks))
                            {
                                MajorRelease = majorVersion,
                                SdkVersion = sdkVersions?.FirstOrDefault()
                            };
                        });
                    })
                    .OrderByDescending(p => p.Version, numericStringComparer)
                    .ToList();

                // Extract CVE IDs from disclosures for root-level quick enumeration
                var monthCveIds = cveSummariesForMonth?.Select(d => d.Id).ToList();

                var monthIndex = new HistoryMonthIndex(
                    HistoryKind.Month,
                    IndexTitles.TimelineMonthTitle(year.Year, month.Month),
                    year.Year,
                    month.Month,
                    monthReleaseDate,
                    cveSummariesForMonth?.Count > 0)
                {
                    PrevMonthDate = previousMonthDate,
                    PrevSecurityMonthDate = previousSecurityMonthDate,
                    CveRecords = monthCveIds?.Count > 0 ? monthCveIds : null,
                    MajorReleases = sortedMonthReleases,
                    Links = HalHelpers.OrderLinks(monthIndexLinks),
                    Embedded = new HistoryMonthIndexEmbedded
                    {
                        Patches = embeddedReleases,
                        Disclosures = cveSummariesForMonth
                    }
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
                await File.WriteAllTextAsync(currentMonthIndexPath, finalMonthIndexJson);

                // Generate month manifest with markdown links
                // Note: HalLinkGenerator automatically adds "(HTML)" suffix for GitHub markdown links
                var cveTitle = $"CVE records - {IndexTitles.FormatMonthYear(year.Year, month.Month)}";
                var cveTitleHtml = $"CVE records (HTML) - {IndexTitles.FormatMonthYear(year.Year, month.Month)}";
                var monthManifestLinks = halLinkGenerator.Generate(
                    monthPath,
                    HistoryManifestFileMappings.Values,
                    (fileLink, key) => key switch
                    {
                        "cve-markdown" => cveTitle,
                        "cve-html" => cveTitleHtml,
                        _ => fileLink.Title
                    },
                    includeSelf: false);

                // Add self link for manifest
                var monthManifestRelativePath = Path.GetRelativePath(inputPath, Path.Combine(monthPath, FileNames.Manifest));
                monthManifestLinks[HalTerms.Self] = new HalLink(urlGenerator(monthManifestRelativePath, LinkStyle.Prod));

                // Read _manifest.json if it exists and merge links
                var partialManifestPath = Path.Combine(inputMonthPath, FileNames.PartialManifest);
                if (File.Exists(partialManifestPath))
                {
                    try
                    {
                        var partialJson = await File.ReadAllTextAsync(partialManifestPath);
                        var partial = JsonSerializer.Deserialize<PartialContentManifest>(partialJson, ReleaseManifestSerializerContext.Default.PartialContentManifest);
                        if (partial?.Links != null)
                        {
                            foreach (var (key, link) in partial.Links)
                            {
                                if (key == HalTerms.Self)
                                    continue;
                                monthManifestLinks[key] = link;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to read {partialManifestPath}: {ex.Message}");
                    }
                }

                // Write month manifest.json if there are any links beyond self
                if (monthManifestLinks.Count > 1)
                {
                    var monthManifest = new ContentManifest(
                        "manifest",
                        $"Manifest - {IndexTitles.FormatMonthYear(year.Year, month.Month)}")
                    {
                        Links = HalHelpers.OrderLinks(monthManifestLinks)
                    };

                    var monthManifestJson = JsonSerializer.Serialize(
                        monthManifest,
                        ReleaseManifestSerializerContext.Default.ContentManifest);

                    // Add schema reference
                    var manifestSchemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.ReleaseManifest}";
                    var updatedManifestJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(monthManifestJson, manifestSchemaUri);

                    var monthManifestPath = Path.Combine(monthPath, FileNames.Manifest);
                    await File.WriteAllTextAsync(monthManifestPath, (updatedManifestJson ?? monthManifestJson) + '\n');
                }

                // Update previousSecurityMonth tracker if this month had security releases
                // This is used for prev-security links in subsequent months
                if (cveSummariesForMonth?.Count > 0)
                {
                    previousSecurityMonth = (year.Year, month.Month);
                    previousSecurityMonthDate = monthReleaseDate;
                }

                // Update previousMonthDate tracker for next month's prev-month-date
                previousMonthDate = monthReleaseDate;
            }

            // Generate the root links for the year index
            var yearHalLinks = halLinkGenerator.Generate(
                yearPath,
                TimelineYearFileMappings.Values,
                (fileLink, key) => key == HalTerms.Self ? IndexTitles.TimelineYearLink(year.Year) : fileLink.Title);

            // Add self link for year index (generated file may not exist yet) - href only
            var yearIndexRelativePath = Path.GetRelativePath(inputPath, Path.Combine(yearPath, FileNames.Index));
            yearHalLinks[HalTerms.Self] = new HalLink(urlGenerator(yearIndexRelativePath, LinkStyle.Prod));

            // Add next/prev links for year navigation (currentYearIndex already calculated above)
            if (currentYearIndex > 0)
            {
                var prevYear = sortedYears[currentYearIndex - 1];
                var prevYearPath = Path.Combine(historyPath, prevYear);
                var prevYearIndexPath = Path.Combine(prevYearPath, FileNames.Index);
                var prevYearIndexRelativePath = Path.GetRelativePath(inputPath, prevYearIndexPath);
                var prevYearPathValue = "/" + prevYearIndexRelativePath.Replace("\\", "/");
                yearHalLinks[LinkRelations.PrevYear] = new HalLink(urlGenerator(prevYearIndexRelativePath, LinkStyle.Prod))
                {
                    Title = $"Previous year - {prevYear}",
                };
            }
            // NOTE: No "next" links - year indexes are immutable after their last natural update.
            // Navigation pattern: start from latest-year and walk backwards via "prev" links.

            // Add timeline-index link (parent)
            yearHalLinks[LinkRelations.Timeline] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{FileNames.Index}")
            {
                Title = IndexTitles.TimelineIndexLink,
            };

            // Get the latest major version for the year
            var yearLatestVersion = releasesForYear.Max(numericStringComparer) ?? "unknown";

            // Calculate latest month for this year (months are ordered chronologically, so last is latest)
            var latestMonth = monthSummaries.LastOrDefault()?.Month;

            // Add latest-month link if available
            if (latestMonth != null)
            {
                var latestMonthPath = Path.Combine(yearPath, latestMonth, FileNames.Index);
                var latestMonthRelativePath = Path.GetRelativePath(inputPath, latestMonthPath);
                var latestMonthPathValue = "/" + latestMonthRelativePath.Replace("\\", "/");
                yearHalLinks[LinkRelations.LatestMonth] = new HalLink(urlGenerator(latestMonthRelativePath, LinkStyle.Prod))
                {
                    Title = $"Latest month - {IndexTitles.FormatMonthYear(year.Year, latestMonth)}",
                };
            }

            // Calculate latest security month for this year (months are ordered chronologically, so search from end)
            var latestSecurityMonthThisYear = monthSummaries.LastOrDefault(m => m.Security)?.Month;

            // Update global latest security month tracker (comparing YYYY-MM strings)
            if (latestSecurityMonthThisYear != null)
            {
                var yearMonthString = $"{year.Year}-{latestSecurityMonthThisYear}";
                if (globalLatestSecurityMonth == null ||
                    string.Compare(yearMonthString, globalLatestSecurityMonth, StringComparison.Ordinal) > 0)
                {
                    globalLatestSecurityMonth = yearMonthString;
                }
            }

            // Add latest-security-month link - use this year's latest, or fall back to previous year's
            // previousSecurityMonth holds the most recent security month processed so far (across all years)
            if (latestSecurityMonthThisYear != null)
            {
                var latestSecurityMonthPath = Path.Combine(yearPath, latestSecurityMonthThisYear, FileNames.Index);
                var latestSecurityMonthRelativePath = Path.GetRelativePath(inputPath, latestSecurityMonthPath);
                yearHalLinks[LinkRelations.LatestSecurityMonth] = new HalLink(urlGenerator(latestSecurityMonthRelativePath, LinkStyle.Prod))
                {
                    Title = $"Latest security month - {IndexTitles.FormatMonthYear(year.Year, latestSecurityMonthThisYear)}",
                };
            }
            else if (previousSecurityMonth != null)
            {
                // No security releases this year yet, fall back to previous year's latest security month
                var latestSecurityMonthPath = Path.Combine(historyPath, previousSecurityMonth.Value.Year, previousSecurityMonth.Value.Month, FileNames.Index);
                var latestSecurityMonthRelativePath = Path.GetRelativePath(inputPath, latestSecurityMonthPath);
                yearHalLinks[LinkRelations.LatestSecurityMonth] = new HalLink(urlGenerator(latestSecurityMonthRelativePath, LinkStyle.Prod))
                {
                    Title = $"Latest security month - {IndexTitles.FormatMonthYear(previousSecurityMonth.Value.Year, previousSecurityMonth.Value.Month)}",
                };
            }

            // Calculate latest release and sorted releases for the year
            var sortedReleasesForYear = releasesForYear
                .OrderByDescending(v => v, numericStringComparer)
                .ToList();
            // Latest release is the highest major version (two-part, e.g., "10.0")
            var latestReleaseForYear = sortedReleasesForYear.FirstOrDefault();

            // Add latest-major link if available
            if (latestReleaseForYear != null)
            {
                var latestReleaseIndexPath = $"{latestReleaseForYear}/{FileNames.Index}";
                yearHalLinks[LinkRelations.LatestMajor] = new HalLink($"{Location.GitHubBaseUri}{latestReleaseIndexPath}")
                {
                    Title = $"Latest major - .NET {latestReleaseForYear}",
                };
            }

            // Determine the effective latest security month (this year or fallback to previous)
            var effectiveLatestSecurityMonth = latestSecurityMonthThisYear ?? previousSecurityMonth?.Month;

            // Create the year index (e.g., release-notes/2025/index.json)
            var yearHistory = new HistoryYearIndex(
                HistoryKind.Year,
                IndexTitles.TimelineYearTitle(year.Year),
                year.Year)
            {
                LatestMonth = latestMonth,
                LatestSecurityMonth = effectiveLatestSecurityMonth,
                LatestMajor = latestReleaseForYear,
                MajorReleases = sortedReleasesForYear.Count > 0 ? sortedReleasesForYear : null,
                Links = HalHelpers.OrderLinks(yearHalLinks)
            };

            yearHistory.Embedded = new HistoryYearIndexEmbedded
            {
                Months = monthSummaries.AsEnumerable().Reverse().ToList()
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
            await File.WriteAllTextAsync(yearIndexPath, finalYearIndexJson);

            // for the overall index

            var overallYearHalLinks = halLinkGenerator.Generate(
                yearPath,
                HistoryFileMappings.Values,
                (fileLink, key) => key == HalTerms.Self ? IndexTitles.TimelineYearLink(year.Year) : fileLink.Title);

            // NOTE: Do NOT add latest-month link here - it changes monthly
            // and would cause the root timeline/index.json to change frequently.
            // The latest-month link belongs in the year-level indexes (e.g., timeline/2025/index.json).

            // Strip titles from all links for embedded year entries - context established by parent
            var minimalYearLinks = overallYearHalLinks.ToDictionary(
                kvp => kvp.Key,
                kvp => new HalLink(kvp.Value.Href) { Type = kvp.Value.Type });

            yearEntries.Add(new HistoryYearEntry(year.Year)
            {
                MajorReleases = [.. sortedReleasesForYear],
                Links = HalHelpers.OrderLinks(minimalYearLinks)
            });
        }

        var fullIndexLinks = halLinkGenerator.Generate(
            historyPath,
            TimelineRootFileMappings.Values,
            (fileLink, key) => key == HalTerms.Self ? IndexTitles.TimelineIndexLink : fileLink.Title);

        // Strip title from self link (href is sufficient)
        if (fullIndexLinks.TryGetValue(HalTerms.Self, out var selfLink))
        {
            fullIndexLinks[HalTerms.Self] = new HalLink(selfLink.Href);
        }

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
        fullIndexLinks[LinkRelations.Root] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Index}")
        {
            Title = IndexTitles.VersionIndexTitle,
        };

        // Add cross-reference links to latest versions (from releases-index)
        if (latestRelease != null)
        {
            fullIndexLinks[LinkRelations.LatestMajor] = new HalLink($"{Location.GitHubBaseUri}{latestRelease.MajorVersion}/{FileNames.Index}")
            {
                Title = $"Latest major release - .NET {latestRelease.MajorVersion}",
            };
        }

        if (latestLtsRelease != null)
        {
            fullIndexLinks[LinkRelations.LatestLtsMajor] = new HalLink($"{Location.GitHubBaseUri}{latestLtsRelease.MajorVersion}/{FileNames.Index}")
            {
                Title = $"Latest LTS major release - .NET {latestLtsRelease.MajorVersion}",
            };
        }

        // Add latest-year link
        if (latestYear != null)
        {
            fullIndexLinks[LinkRelations.LatestYear] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{latestYear}/{FileNames.Index}")
            {
                Title = $"Latest year - {latestYear}",
            };
        }

        // NOTE: Do NOT add latest-security-month link here - it changes frequently
        // and would cause the root timeline/index.json to change with every security release.
        // Users can find security releases by navigating through year -> month indexes.

        // Get the latest major version for the root history index
        var rootLatestVersion = allReleases.Max(numericStringComparer) ?? "unknown";

        // Create the history index
        var historyIndex = new ReleaseHistoryIndex(
            HistoryKind.Timeline,
            IndexTitles.TimelineIndexTitle)
        {
            LatestYear = latestYear,
            LatestMajor = latestRelease?.MajorVersion,
            LatestLtsMajor = latestLtsRelease?.MajorVersion,
            Links = HalHelpers.OrderLinks(fullIndexLinks),
            Embedded = new ReleaseHistoryIndexEmbedded
            {
                Years = [.. yearEntries.OrderByDescending(e => e.Year, StringComparer.OrdinalIgnoreCase)]
            }
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
        await File.WriteAllTextAsync(historyIndexPath, finalHistoryIndexJson);
    }
}
