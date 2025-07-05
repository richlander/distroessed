using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DotnetRelease;

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

        List<HistoryYearEntry> yearEntries = [];


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
                var historyLinks = HalHelpers.GetHalLinksForPath(
                    monthPath,
                    new PathContext(monthPath, rootPath),
                    true,
                    HistoryFileMappings.Values);

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
                var entry = new HistoryMonthEntry(month.Month, historyLinks, releases.ToList())
                {
                    CveRecords = cveRecords?.Records.Select(r => new CveRecordSummary(r.Id, r.Title)
                    {
                        Href = r.References?.FirstOrDefault()
                    }).ToList()
                };
                monthEntries.Add(entry);
            }

            var yearHalLinks = HalHelpers.GetHalLinksForPath(
                yearPath,
                new PathContext(yearPath, rootPath),
                true,
                HistoryFileMappings.Values);

            yearHalLinks["self"].Title = $"Release history for {year.Year}";

            // Create the year index (e.g., release-notes/2025/index.json)
            var yearHistory = new HistoryYearIndex(
                HistoryKind.HistoryYearIndex,
                $"Release history for {year.Year}",
                year.Year,
                yearHalLinks);
            yearHistory.Embedded = new MonthIndexEmbedded(monthEntries);
            yearHistory.ReleaseNotes = releasesForYear.Select(version => new ReleaseMetadata(
                version,
                $"https://github.com/dotnet/core/blob/main/release-notes/{version}/README.md",
                $".NET {version} Release notes",
                "text/html"
            )).ToList();

            using Stream yearStream = File.Create(Path.Combine(yearPath, "index.json"));
            JsonSerializer.Serialize(
                yearStream,
                yearHistory,
                HistoryYearIndexSerializerContext.Default.HistoryYearIndex);

            // for the overall index

            var overallYearHalLinks = HalHelpers.GetHalLinksForPath(
                yearPath,
                new PathContext(yearPath, rootPath),
                true,
                HistoryFileMappings.Values);

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

        var fullIndexLinks = HalHelpers.GetHalLinksForPath(
            rootPath,
            new PathContext(rootPath),
            true,
            HistoryFileMappings.Values);

        // Create the history index
        var historyIndex = new HistoryIndex(
            HistoryKind.HistoryIndex,
            "History of .NET releases",
            fullIndexLinks
            )
        {
            Embedded = new YearIndexEmbedded([.. yearEntries.OrderByDescending(e => e.Year, StringComparer.OrdinalIgnoreCase)])
        };

        using var historyStream = File.Create(Path.Combine(rootPath, "index.json"));
        JsonSerializer.Serialize(
            historyStream,
            historyIndex,
            HistoryIndexSerializerContext.Default.HistoryIndex);
    }
}
