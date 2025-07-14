using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DotnetRelease;
using System.Linq;
using System.Globalization;

namespace UpdateIndexes;

public class HistoryIndexFiles
{

    public static readonly Dictionary<string, FileLink> HistoryFileMappings = new()
    {
        {"index.json", new FileLink("index.json", "History Index", LinkStyle.Prod) },
        {"cve.json", new FileLink("cve.json", "CVE Information", LinkStyle.Prod) },
        {"cve.md", new FileLink("cve.md", "CVE Information", LinkStyle.Prod | LinkStyle.GitHub) },
    };

    public static readonly Dictionary<string, FileLink> ReleaseFileMappings = new()
    {
        {"index.json", new FileLink("index.json", ".NET Release Index", LinkStyle.Prod) },
        {"README.md", new FileLink("README.md", ".NET Release Notes", LinkStyle.GitHub) },
    };

    public static async Task GenerateAsync(string rootPath, ReleaseHistory releaseHistory)
    {
        var historyPath = Path.Combine(rootPath, "history");

        if (!Directory.Exists(historyPath))
        {
            Directory.CreateDirectory(historyPath);
        }

        var numericStringComparer = StringComparer.OrdinalIgnoreCase;
        
        var urlGenerator = (string relativePath, LinkStyle style) => style == LinkStyle.Prod 
            ? $"https://raw.githubusercontent.com/richlander/core/main/release-notes/{relativePath}"
            : $"https://github.com/dotnet/core/blob/main/release-notes/{relativePath}";
        

        var halLinkGenerator = new HalLinkGenerator(rootPath, urlGenerator);
        
        List<HistoryYearEntry> yearEntries = [];

        HashSet<string> allReleases = [];

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
                    (fileLink, key) => key == HalTerms.Self ? $"Release history for {year.Year}-{month.Month}" : fileLink.Title);

                HashSet<string> monthReleases = [];
                HashSet<string> monthPatchReleases = [];

                // Process each day in the month
                foreach (var days in month.Days.Values)
                {
                    foreach (var day in days.Releases)
                    {
                        monthReleases.Add(day.MajorVersion);
                        releasesForYear.Add(day.MajorVersion);
                        allReleases.Add(day.MajorVersion);
                        
                        // Add patch version (e.g., "10.0.1")
                        monthPatchReleases.Add(day.PatchVersion);
                        
                        // Add runtime and SDK versions from components
                        foreach (var component in day.Components)
                        {
                            if (component.Name == "Runtime" || component.Name == "SDK")
                            {
                                monthPatchReleases.Add(component.Version);
                            }
                        }
                    }
                }

                // Load CVE information for the month
                var cveJsonPath = Path.Combine(monthPath, "cve.json");
                CveRecords? cveRecords = null;

                if (File.Exists(cveJsonPath))
                {
                    using var cveStream = File.OpenRead(cveJsonPath);
                    cveRecords = await JsonSerializer.DeserializeAsync<CveRecords>(cveStream, CveInfoSerializerContext.Default.CveRecords);
                }

                // Prepare month index path for links
                var monthIndexPath = Path.Combine(monthPath, "index.json");
                var monthIndexRelativePath = Path.GetRelativePath(rootPath, monthIndexPath);

                // Create simplified month summary for year index with proper self link and CVE links
                var monthSummaryLinks = new Dictionary<string, HalLink>
                {
                    [HalTerms.Self] = new HalLink(urlGenerator(monthIndexRelativePath, LinkStyle.Prod))
                    {
                        Relative = monthIndexRelativePath,
                        Title = $"Release history for {year.Year}-{month.Month}",
                        Type = MediaType.HalJson
                    }
                };

                // Add CVE JSON link if CVE records exist
                if (cveRecords?.Records.Count > 0)
                {
                    var cveJsonRelativePath = Path.GetRelativePath(rootPath, Path.Combine(monthPath, "cve.json"));
                    
                    monthSummaryLinks["cve-json"] = new HalLink(urlGenerator(cveJsonRelativePath, LinkStyle.Prod))
                    {
                        Relative = cveJsonRelativePath,
                        Title = "CVE Information",
                        Type = MediaType.Json
                    };
                }

                var monthSummary = new HistoryMonthSummary(
                    month.Month,
                    monthSummaryLinks,
                    cveRecords?.Records.Select(r => new CveRecordSummary(r.Id, r.Title)
                    {
                        Href = r.References?.FirstOrDefault()
                    }).ToList() ?? [],
                    [.. monthReleases]
                );
                monthSummaries.Add(monthSummary);

                // Create detailed month index with proper self link
                var monthIndexLinks = new Dictionary<string, HalLink>(monthHistoryLinks)
                {
                    [HalTerms.Self] = new HalLink(urlGenerator(monthIndexRelativePath, LinkStyle.Prod))
                    {
                        Relative = monthIndexRelativePath,
                        Title = $"Release history for {year.Year}-{month.Month}",
                        Type = MediaType.HalJson
                    }
                };

                var monthIndex = new HistoryMonthIndex(
                    HistoryKind.HistoryMonthIndex,
                    $"Release history for {year.Year}-{month.Month}",
                    year.Year,
                    month.Month,
                    monthIndexLinks)
                {
                    Schema = SchemaUrls.HistoryMonthIndex,
                    Embedded = new HistoryMonthIndexEmbedded
                    {
                        DotnetReleases = [.. monthReleases.OrderByDescending(v => v, numericStringComparer)],
                        DotnetPatchReleases = [.. monthPatchReleases.OrderByDescending(v => v, numericStringComparer)]
                    }
                };

                // Write monthly index file
                using Stream monthStream = File.Create(Path.Combine(monthPath, "index.json"));
                JsonSerializer.Serialize(
                    monthStream,
                    monthIndex,
                    HistoryYearIndexSerializerContext.Default.HistoryMonthIndex);
            }

            // Generate the root links for the year index
            var yearHalLinks = halLinkGenerator.Generate(
                yearPath,
                HistoryFileMappings.Values,
                (fileLink, key) => key == HalTerms.Self ? $"Release history for {year.Year}" : fileLink.Title);

            // Create the year index (e.g., release-notes/2025/index.json)
            var yearHistory = new HistoryYearIndex(
                HistoryKind.HistoryYearIndex,
                $"Release history for {year.Year}",
                year.Year,
                yearHalLinks)
            {
                Schema = SchemaUrls.HistoryYearIndex
            };
            // Create embedded releases structure
            var releaseEntries = new List<HistoryReleaseIndexEntry>(
                releasesForYear
                    .OrderByDescending(v => v, numericStringComparer)
                    .Select(version => new HistoryReleaseIndexEntry(version, yearHalLinks))
            );
            
            yearHistory.Embedded = new HistoryYearIndexEmbedded
            {
                Months = monthSummaries,
                Releases = releaseEntries
            };

            using Stream yearStream = File.Create(Path.Combine(yearPath, "index.json"));
            JsonSerializer.Serialize(
                yearStream,
                yearHistory,
                HistoryYearIndexSerializerContext.Default.HistoryYearIndex);

            // for the overall index

            var overallYearHalLinks = halLinkGenerator.Generate(
                yearPath,
                HistoryFileMappings.Values,
                (fileLink, key) => key == HalTerms.Self ? $"Release history for {year.Year}" : fileLink.Title);

            yearEntries.Add(new HistoryYearEntry(
                HistoryKind.HistoryYearIndex,
                $".NET release history for {year.Year}",
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
            (fileLink, key) => key == HalTerms.Self ? "History of .NET releases" : fileLink.Title);

        // Create embedded releases structure
        var rootReleaseEntries = allReleases
            .OrderByDescending(v => v, numericStringComparer)
            .Select(version => new HistoryReleaseIndexEntry(version, fullIndexLinks))
            .ToList();

        // Create the history index
        var historyIndex = new HistoryIndex(
            HistoryKind.HistoryIndex,
            "History of .NET releases",
            fullIndexLinks
            )
        {
            Schema = SchemaUrls.HistoryIndex,
            Embedded = new HistoryIndexEmbedded
            {
                Years = [.. yearEntries.OrderByDescending(e => e.Year, StringComparer.OrdinalIgnoreCase)],
                Releases = rootReleaseEntries
            }
        };

        using var historyStream = File.Create(Path.Combine(historyPath, "index.json"));
        JsonSerializer.Serialize(
            historyStream,
            historyIndex,
            HistoryIndexSerializerContext.Default.HistoryIndex);
    }
}
