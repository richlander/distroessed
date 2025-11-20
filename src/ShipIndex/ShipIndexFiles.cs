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
        {"index.json", new FileLink("index.json", LinkTitles.HistoryIndex, LinkStyle.Prod) },
        {"cve.json", new FileLink("cve.json", LinkTitles.CveInformation, LinkStyle.Prod) },
        {"cve.md", new FileLink("cve.md", LinkTitles.CveInformation, LinkStyle.Prod | LinkStyle.GitHub) },
    };

    public static readonly OrderedDictionary<string, FileLink> ReleaseFileMappings = new()
    {
        {"index.json", new FileLink("index.json", LinkTitles.DotNetReleaseIndex, LinkStyle.Prod) },
        {"README.md", new FileLink("README.md", LinkTitles.DotNetReleaseNotes, LinkStyle.GitHub) },
    };

    public static async Task GenerateAsync(string inputPath, string outputPath, ReleaseHistory releaseHistory)
    {
        var historyPath = Path.Combine(outputPath, "timeline");

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

            // Get sorted list of months for next/prev links
            var sortedMonths = year.Months.Keys.OrderBy(m => m, numericStringComparer).ToList();

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
                var monthIndexPath = Path.Combine(monthPath, "index.json");
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
                    var cveJsonRelativePath = Path.GetRelativePath(inputPath, Path.Combine(monthPath, "cve.json"));
                    var cveJsonPathValue = "/" + cveJsonRelativePath.Replace("\\", "/");

                    monthSummaryLinks["cve-json"] = new HalLink(urlGenerator(cveJsonRelativePath, LinkStyle.Prod))
                    {
                        Path = cveJsonPathValue,
                        Title = LinkTitles.CveInformation,
                        Type = MediaType.Json
                    };
                }

                var monthSummary = new HistoryMonthSummary(
                    month.Month,
                    monthSummaryLinks,
                    cveSummariesForMonth?.Select(s => s.Id).ToList(),
                    [.. monthReleases]
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

                // Add next/prev links for month navigation
                var currentMonthIndex = sortedMonths.IndexOf(month.Month);
                if (currentMonthIndex > 0)
                {
                    var prevMonth = sortedMonths[currentMonthIndex - 1];
                    var prevMonthIndexPath = Path.Combine(yearPath, prevMonth, "index.json");
                    var prevMonthIndexRelativePath = Path.GetRelativePath(inputPath, prevMonthIndexPath);
                    var prevMonthPathValue = "/" + prevMonthIndexRelativePath.Replace("\\", "/");
                    monthIndexLinks[HalTerms.Prev] = new HalLink(urlGenerator(prevMonthIndexRelativePath, LinkStyle.Prod))
                    {
                        Path = prevMonthPathValue,
                        Title = IndexTitles.TimelineMonthLink(year.Year, prevMonth),
                        Type = MediaType.HalJson
                    };
                }
                if (currentMonthIndex < sortedMonths.Count - 1)
                {
                    var nextMonth = sortedMonths[currentMonthIndex + 1];
                    var nextMonthIndexPath = Path.Combine(yearPath, nextMonth, "index.json");
                    var nextMonthIndexRelativePath = Path.GetRelativePath(inputPath, nextMonthIndexPath);
                    var nextMonthPathValue = "/" + nextMonthIndexRelativePath.Replace("\\", "/");
                    monthIndexLinks[HalTerms.Next] = new HalLink(urlGenerator(nextMonthIndexRelativePath, LinkStyle.Prod))
                    {
                        Path = nextMonthPathValue,
                        Title = IndexTitles.TimelineMonthLink(year.Year, nextMonth),
                        Type = MediaType.HalJson
                    };
                }

                // Calculate version range for month index
                var monthMinVersion = monthReleases.Min(numericStringComparer);
                var monthMaxVersion = monthReleases.Max(numericStringComparer);
                var monthVersionRange = $"{monthMinVersion}–{monthMaxVersion}";

                var monthIndex = new HistoryMonthIndex(
                    HistoryKind.TimelineMonthIndex,
                    IndexTitles.TimelineMonthTitle(year.Year, month.Month),
                    IndexTitles.TimelineMonthIndexDescription(year.Year, month.Month, monthVersionRange, Location.CacheFriendlyNote),
                    year.Year,
                    month.Month,
                    monthIndexLinks)
                {
                    Embedded = new HistoryMonthIndexEmbedded
                    {
                        Releases = releasesByMajor
                            .OrderByDescending(kv => kv.Key, numericStringComparer)
                            .ToDictionary(
                                kv => kv.Key,
                                kv => 
                                {
                                    var majorVersion = kv.Key;
                                    var versionIndexPath = $"{majorVersion}/index.json";
                                    
                                    // Collect runtime and SDK patches separately
                                    var runtimePatches = kv.Value.Keys.OrderByDescending(v => v, numericStringComparer).ToList();
                                    var sdkPatches = kv.Value.Values
                                        .SelectMany(p => p.SdkVersions)
                                        .Distinct()
                                        .OrderByDescending(v => v, numericStringComparer)
                                        .ToList();
                                    
                                    var patchesDict = new Dictionary<string, IList<string>>
                                    {
                                        ["dotnet-runtime"] = runtimePatches
                                    };
                                    
                                    if (sdkPatches.Count > 0)
                                    {
                                        patchesDict["dotnet-sdk"] = sdkPatches;
                                    }
                                    
                                    var links = new Dictionary<string, object>
                                    {
                                        ["version-index"] = new HalLink($"{Location.GitHubBaseUri}{versionIndexPath}")
                                        {
                                            Path = "/" + versionIndexPath,
                                            Title = $".NET {majorVersion} Version Index",
                                            Type = MediaType.HalJson
                                        }
                                    };

                                    // Add individual release links for each runtime version
                                    foreach (var runtimeVersion in runtimePatches)
                                    {
                                        var patchInfo = kv.Value[runtimeVersion];
                                        var releaseJsonPath = $"{majorVersion}/{patchInfo.PatchVersion}/release.json";
                                        
                                        links[runtimeVersion] = new HalLink($"{Location.GitHubBaseUri}{releaseJsonPath}")
                                        {
                                            Path = "/" + releaseJsonPath,
                                            Title = $".NET {runtimeVersion} Release",
                                            Type = MediaType.Json
                                        };

                                        // Add markdown link
                                        var readmePath = $"{majorVersion}/{patchInfo.PatchVersion}/README.md";
                                        links[$"{runtimeVersion}-markdown-raw"] = new HalLink($"{Location.GitHubBaseUri}{readmePath}")
                                        {
                                            Path = "/" + readmePath,
                                            Title = $".NET {runtimeVersion} Release Notes (Raw Markdown)",
                                            Type = MediaType.Markdown
                                        };
                                    }

                                    // Add SDK index link for supported versions (8.0+)
                                    if (int.TryParse(majorVersion.Split('.')[0], out int majorVersionNumber) && majorVersionNumber >= 8)
                                    {
                                        var sdkIndexPath = $"{majorVersion}/sdk/index.json";
                                        links["sdk-index"] = new HalLink($"{Location.GitHubBaseUri}{sdkIndexPath}")
                                        {
                                            Path = "/" + sdkIndexPath,
                                            Title = $".NET SDK {majorVersion} Release Information",
                                            Type = MediaType.HalJson
                                        };

                                        // Add links to SDK feature band JSON files
                                        var sdkFeatureBands = sdkPatches
                                            .Select(v => {
                                                var parts = v.Split('.');
                                                if (parts.Length >= 3)
                                                {
                                                    return $"{parts[0]}.{parts[1]}.{parts[2][0]}xx";
                                                }
                                                return null;
                                            })
                                            .Where(fb => fb != null)
                                            .Distinct()
                                            .OrderByDescending(fb => fb);

                                        foreach (var featureBand in sdkFeatureBands)
                                        {
                                            var sdkBandPath = $"{majorVersion}/sdk/sdk-{featureBand}.json";
                                            links[$"sdk-{featureBand}"] = new HalLink($"{Location.GitHubBaseUri}{sdkBandPath}")
                                            {
                                                Path = "/" + sdkBandPath,
                                                Title = $".NET SDK {featureBand}",
                                                Type = MediaType.Json
                                            };
                                        }
                                    }

                                    // Filter CVE IDs for this major version
                                    IList<string>? majorVersionCveIds = null;
                                    if (cveSummariesForMonth != null)
                                    {
                                        majorVersionCveIds = cveSummariesForMonth
                                            .Where(cve => cve.AffectedReleases?.Contains(majorVersion) == true)
                                            .Select(cve => cve.Id)
                                            .ToList();
                                        
                                        if (majorVersionCveIds.Count == 0)
                                        {
                                            majorVersionCveIds = null;
                                        }
                                    }

                                    var majorReleaseHistory = new MajorReleaseHistory(patchesDict)
                                    {
                                        CveRecords = majorVersionCveIds,
                                        Links = links
                                    };
                                    return majorReleaseHistory;
                                }),
                        Disclosures = cveSummariesForMonth
                    },
                    Metadata = new GenerationMetadata("1.0", DateTimeOffset.UtcNow, "ShipIndex")
                };

                // Serialize to string first to add schema reference
                var monthIndexJson = JsonSerializer.Serialize(
                    monthIndex,
                    HistoryYearIndexSerializerContext.Default.HistoryMonthIndex);

                // Add schema reference
                var monthSchemaUri = $"{Location.GitHubBaseUri}schemas/dotnet-release-timeline-index.json";
                var updatedMonthIndexJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(monthIndexJson, monthSchemaUri);

                // Write monthly index file
                var currentMonthIndexPath = Path.Combine(monthPath, "index.json");
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

            // Add next/prev links for year navigation
            var currentYearIndex = sortedYears.IndexOf(year.Year);
            if (currentYearIndex > 0)
            {
                var prevYear = sortedYears[currentYearIndex - 1];
                var prevYearPath = Path.Combine(historyPath, prevYear);
                var prevYearIndexPath = Path.Combine(prevYearPath, "index.json");
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
                var nextYearIndexPath = Path.Combine(nextYearPath, "index.json");
                var nextYearIndexRelativePath = Path.GetRelativePath(inputPath, nextYearIndexPath);
                var nextYearPathValue = "/" + nextYearIndexRelativePath.Replace("\\", "/");
                yearHalLinks[HalTerms.Next] = new HalLink(urlGenerator(nextYearIndexRelativePath, LinkStyle.Prod))
                {
                    Path = nextYearPathValue,
                    Title = IndexTitles.TimelineYearLink(nextYear),
                    Type = MediaType.HalJson
                };
            }

            // Calculate version range for year index
            var yearMinVersion = releasesForYear.Min(numericStringComparer);
            var yearMaxVersion = releasesForYear.Max(numericStringComparer);
            var yearVersionRange = $"{yearMinVersion}–{yearMaxVersion}";

            // Create the year index (e.g., release-notes/2025/index.json)
            var yearHistory = new HistoryYearIndex(
                HistoryKind.TimelineYearIndex,
                IndexTitles.TimelineYearTitle(year.Year),
                IndexTitles.TimelineYearIndexDescription(year.Year, yearVersionRange, Location.CacheFriendlyNote),
                year.Year,
                yearHalLinks)
            {
                Metadata = new GenerationMetadata("1.0", DateTimeOffset.UtcNow, "ShipIndex")
            };

            // Create embedded releases structure
            var releaseEntries = new List<ReleaseHistoryIndexEntry>(
                releasesForYear
                    .OrderByDescending(v => v, numericStringComparer)
                    .Select(version => new ReleaseHistoryIndexEntry(version, yearHalLinks))
            );

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
            var yearSchemaUri = $"{Location.GitHubBaseUri}schemas/dotnet-release-timeline-index.json";
            var updatedYearIndexJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(yearIndexJson, yearSchemaUri);

            var yearIndexPath = Path.Combine(yearPath, "index.json");
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

            yearEntries.Add(new HistoryYearEntry(
                HistoryKind.TimelineYearIndex,
                IndexTitles.TimelineYearDescription(year.Year),
                year.Year,
                overallYearHalLinks)
            {
                DotnetReleases = [.. releasesForYear]
            }
            );
        }

        var fullIndexLinks = halLinkGenerator.Generate(
            historyPath,
            HistoryFileMappings.Values,
            (fileLink, key) => key == HalTerms.Self ? IndexTitles.TimelineIndexLink : fileLink.Title);

        // Add release-version-index link pointing back to root index.json
        fullIndexLinks["release-version-index"] = new HalLink($"{Location.GitHubBaseUri}index.json")
        {
            Path = "/index.json",
            Title = IndexTitles.VersionIndexTitle,
            Type = MediaType.HalJson
        };

        // Calculate version range for root history index
        var minVersion = allReleases.Min(numericStringComparer);
        var maxVersion = allReleases.Max(numericStringComparer);
        var rootVersionRange = $"{minVersion}–{maxVersion}";

        // Create the history index
        var historyIndex = new ReleaseHistoryIndex(
            HistoryKind.ReleaseTimelineIndex,
            IndexTitles.TimelineIndexTitle,
            IndexTitles.TimelineIndexDescription(rootVersionRange, Location.CacheFriendlyNote),
            fullIndexLinks
            )
        {
            Glossary = new Dictionary<string, string>
            {
                ["lts"] = "Long-Term Support – 3-year support window",
                ["sts"] = "Standard-Term Support – 18-month support window",
                ["cve"] = "Common Vulnerabilities and Exposures – Security vulnerability identifiers",
                ["cvss"] = "Common Vulnerability Scoring System – Vulnerability severity ratings",
                ["release"] = "General Availability – Production-ready release",
                ["eol"] = "End of Life – No longer supported",
                ["preview"] = "Pre-release phase with previews and release candidates",
                ["active"] = "Full support with regular updates and security fixes"
            },
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
        var historySchemaUri = $"{Location.GitHubBaseUri}schemas/dotnet-release-timeline-index.json";
        var updatedHistoryIndexJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(historyIndexJson, historySchemaUri);

        var historyIndexPath = Path.Combine(historyPath, "index.json");
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
