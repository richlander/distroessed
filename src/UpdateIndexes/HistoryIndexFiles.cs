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

    public static readonly OrderedDictionary<string, FileLink> ReleaseFileMappings = new()
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

        var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
        
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

            List<HistoryMonthEntry> monthEntries = [];

            HashSet<string> releasesForYear = [];

            foreach (var month in year.Months.Values)
            {
                Console.WriteLine($"Processing month: {month.Month} in year: {year.Year}");
                var monthPath = Path.Combine(yearPath, month.Month);
                var monthHistoryLinks = halLinkGenerator.Generate(
                    monthPath,
                    HistoryFileMappings.Values,
                    (fileLink, key) => key == HalTerms.Self ? $"Release history for {year.Year}-{month.Month}" : fileLink.Title);

                HashSet<string> monthReleases = [];
                HashSet<string> monthPatchReleases = [];

                // Get month information to add to the year index
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

                // Add to month entries for the year index
                var entry = new HistoryMonthEntry(
                    month.Month,
                    monthHistoryLinks,
                    cveRecords?.Records.Select(r => new CveRecordSummary(r.Id, r.Title)
                    {
                        Href = r.References?.FirstOrDefault()
                    }).ToList() ?? [],
                    [.. monthReleases],
                    [.. monthPatchReleases.OrderByDescending(v => v, numericStringComparer)]
                );
                monthEntries.Add(entry);
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
                yearHalLinks);
            // Create embedded releases structure
            var releaseEntries = new List<HistoryReleaseIndexEntry>(
                releasesForYear
                    .OrderByDescending(v => v, numericStringComparer)
                    .Select(version => new HistoryReleaseIndexEntry(version, yearHalLinks))
            );
            
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
