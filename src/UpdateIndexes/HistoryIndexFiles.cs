using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DotnetRelease;
using System.Linq;
using System.Globalization;

namespace UpdateIndexes;

public class HistoryIndexFiles
{

    public static readonly OrderedDictionary<string, FileLink> HistoryFileMappings = new()
    {
        {"index.json", new FileLink("index.json", "History Index", LinkStyle.Prod) },
        {"cve.json", new FileLink("cve.json", "CVE Information", LinkStyle.Prod) },
        {"cve.md", new FileLink("cve.md", "CVE Information", LinkStyle.Prod | LinkStyle.GitHub) },
    };

    public static async Task GenerateAsync(string rootPath, ReleaseHistory releaseHistory)
    {
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Root directory does not exist: {rootPath}");
        }

        var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
        
        var halLinkGenerator = new HalLinkGenerator(rootPath, Path.GetDirectoryName(rootPath)!);
        
        var urlGenerator = (string relativePath, LinkStyle style) => style == LinkStyle.Prod 
            ? $"https://raw.githubusercontent.com/richlander/core/main/release-notes/{relativePath}"
            : $"https://github.com/dotnet/core/blob/main/release-notes/{relativePath}";
        
        List<HistoryYearEntry> yearEntries = [];

        HashSet<string> allReleases = [];

        foreach (var year in releaseHistory.Years.Values)
        {
            Console.WriteLine($"Processing year: {year.Year}");
            var yearPath = Path.Combine(rootPath, year.Year);
            if (!Directory.Exists(yearPath))
            {
                Directory.CreateDirectory(yearPath);
            }

            List<HistoryMonthEntry> monthEntries = [];

            HashSet<string> releasesForYear = [];

            foreach (var month in year.Months.Values)
            {
                Console.WriteLine($"Processing month: {month.Month} in year: {year.Year}");
                var monthPath = Path.Combine(yearPath, month.Month);
                var monthHistoryLinks = halLinkGenerator.Generate(
                    monthPath,
                    HistoryFileMappings.Values,
                    fileLink => fileLink.Title,
                    urlGenerator);

                HashSet<string> releases = [];

                foreach (var days in month.Days.Values)
                {
                    foreach (var day in days.Releases)
                    {
                        // if (day is null)
                        // {
                        //     Console.WriteLine($"No release information found for day in {monthPath}");
                        //     continue;
                        // }
                        // int dayCveCount = day.CveList?.Count ?? 0;
                        // var r = new HistoryRelease(day.MajorVersion, dayCveCount, []);
                        releases.Add(day.MajorVersion);
                        releasesForYear.Add(day.MajorVersion);
                        allReleases.Add(day.MajorVersion);
                    }
                }

                var cveJsonPath = Path.Combine(monthPath, "cve.json");
                CveRecords? cveRecords = null;

                if (File.Exists(cveJsonPath))
                {
                    using var cveStream = File.OpenRead(cveJsonPath);
                    cveRecords = await JsonSerializer.DeserializeAsync<CveRecords>(cveStream, CveInfoSerializerContext.Default.CveRecords);
                }

                // Add to month entries for the year index
                var entry = new HistoryMonthEntry(month.Month, monthHistoryLinks, releases.ToList())
                {
                    CveRecords = cveRecords?.Records.Select(r => new CveRecordSummary(r.Id, r.Title)
                    {
                        Href = r.References?.FirstOrDefault()
                    }).ToList()
                };
                monthEntries.Add(entry);
            }

            var yearHalLinks = halLinkGenerator.Generate(
                yearPath,
                HistoryFileMappings.Values,
                fileLink => fileLink.Title,
                urlGenerator);

            yearHalLinks["self"].Title = $"Release history for {year.Year}";

            // Create the year index (e.g., release-notes/2025/index.json)
            var yearHistory = new HistoryYearIndex(
                HistoryKind.HistoryYearIndex,
                $"Release history for {year.Year}",
                year.Year,
                yearHalLinks);
            // Create embedded releases structure
            var releaseEntries = releasesForYear
                .OrderByDescending(v => v, numericStringComparer)
                .Select(version => new HistoryReleaseIndexEntry(version, new Dictionary<string, HalLink>
                {
                    ["index"] = new HalLink($"https://raw.githubusercontent.com/richlander/core/main/release-notes/{version}/index.json")
                    {
                        Title = $".NET {version} Release Index",
                        Type = "application/hal+json"
                    },
                    ["readme"] = new HalLink($"https://github.com/dotnet/core/blob/main/release-notes/{version}/README.md")
                    {
                        Title = $".NET {version} Release Notes",
                        Type = "text/html"
                    }
                }))
                .ToList();
            
            yearHistory.Embedded = new HistoryYearIndexEmbedded
            {
                Months = monthEntries,
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
                fileLink => fileLink.Title,
                urlGenerator);

            yearEntries.Add(new HistoryYearEntry(
                HistoryKind.HistoryYearIndex,
                $".NET release history for {year.Year}",
                year.Year,
                overallYearHalLinks)
            {
                DotnetReleases = releasesForYear.ToList()
            }
            );
        }

        var fullIndexLinks = halLinkGenerator.Generate(
            rootPath,
            HistoryFileMappings.Values,
            fileLink => fileLink.Title,
            urlGenerator);

        // Create embedded releases structure
        var rootReleaseEntries = allReleases
            .OrderByDescending(v => v, numericStringComparer)
            .Select(version => new HistoryReleaseIndexEntry(version, new Dictionary<string, HalLink>
            {
                ["index"] = new HalLink($"https://raw.githubusercontent.com/richlander/core/main/release-notes/{version}/index.json")
                {
                    Title = $".NET {version} Release Index",
                    Type = "application/hal+json"
                },
                ["readme"] = new HalLink($"https://github.com/dotnet/core/blob/main/release-notes/{version}/README.md")
                {
                    Title = $".NET {version} Release Notes",
                    Type = "text/html"
                }
            }))
            .ToList();

        // Create the history index
        var historyIndex = new HistoryIndex(
            HistoryKind.HistoryIndex,
            "History of .NET releases",
            fullIndexLinks
            )
        {
            Embedded = new HistoryIndexEmbedded
            {
                Years = [.. yearEntries.OrderByDescending(e => e.Year, StringComparer.OrdinalIgnoreCase)],
                Releases = rootReleaseEntries
            }
        };

        using var historyStream = File.Create(Path.Combine(rootPath, "index.json"));
        JsonSerializer.Serialize(
            historyStream,
            historyIndex,
            HistoryIndexSerializerContext.Default.HistoryIndex);
    }
}
