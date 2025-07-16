using System.Globalization;
using System.Text.Json;
using DotnetRelease;
using JsonSchemaInjector;

namespace UpdateIndexes;

public class ReleaseIndexFiles
{
    public static readonly OrderedDictionary<string, FileLink> MainFileMappings = new()
    {
        {"index.json", new FileLink("index.json", "Index", LinkStyle.Prod) },
        {"releases.json", new FileLink("releases.json", "Releases", LinkStyle.Prod) },
        {"release.json", new FileLink("release.json", "Release", LinkStyle.Prod) },
        {"manifest.json", new FileLink("manifest.json", "Manifest", LinkStyle.Prod) },
        {"usage.md", new FileLink("usage.md", "Usage", LinkStyle.Prod | LinkStyle.GitHub) },
        {"terminology.md", new FileLink("terminology.md", "Terminology", LinkStyle.Prod | LinkStyle.GitHub) }
    };

    public static readonly OrderedDictionary<string, FileLink> AuxFileMappings = new()
    {
        {"supported-os.json", new FileLink("supported-os.json", "Supported OSes", LinkStyle.Prod) },
        {"supported-os.md", new FileLink("supported-os.md", "Supported OSes", LinkStyle.Prod | LinkStyle.GitHub) },
        {"linux-packages.json", new FileLink("linux-packages.json", "Linux Packages", LinkStyle.Prod) },
        {"linux-packages.md", new FileLink("linux-packages.md", "Linux Packages", LinkStyle.Prod | LinkStyle.GitHub) },
        {"README.md", new FileLink("README.md", "Release Notes", LinkStyle.GitHub) }
    };

    private readonly List<string> _leafFiles = ["releases.json", "release.json", "manifest.json"];

    // Generates index files for each major version directory and one root index file
    public static async Task GenerateAsync(List<MajorReleaseSummary> summaries, string rootDir, ReleaseHistory? releaseHistory = null)
    {
        if (!Directory.Exists(rootDir))
        {
            throw new DirectoryNotFoundException($"Root directory does not exist: {rootDir}");
        }

        var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
        List<ReleaseVersionIndexEntry> majorEntries = [];

        var summaryTable = summaries.ToDictionary(
            s => s.MajorVersion,
            s => s,
            StringComparer.OrdinalIgnoreCase);

        var urlGenerator = (string relativePath, LinkStyle style) => style == LinkStyle.Prod 
            ? $"https://raw.githubusercontent.com/richlander/core/main/release-notes/{relativePath}"
            : $"https://github.com/dotnet/core/blob/main/release-notes/{relativePath}";

        var halLinkGenerator = new HalLinkGenerator(rootDir, urlGenerator);
        
        // Look at all the major version directories
        // The presence of a releases.json file indicates this is a major version directory
        foreach (var majorVersionDir in Directory.EnumerateDirectories(rootDir))
        {
            var majorVersionDirName = Path.GetFileName(majorVersionDir);

            if (!summaryTable.TryGetValue(majorVersionDirName, out var summary))
            {
                continue;
            }

            var majorVersionLinks = halLinkGenerator.Generate(
                majorVersionDir,
                MainFileMappings.Values,
                (fileLink, key) => key == HalTerms.Self ? summary.MajorVersionLabel : fileLink.Title);

            // Generate patch version index; release-notes/8.0/index.json
            var patchEntries = GetPatchIndexEntries(summaryTable[majorVersionDirName].PatchReleases, new(majorVersionDir, rootDir), releaseHistory);

            var auxLinks = halLinkGenerator.Generate(
                majorVersionDir,
                AuxFileMappings.Values,
                (fileLink, key) => fileLink.Title);

            // Merge aux links into major version links
            foreach (var auxLink in auxLinks)
            {
                majorVersionLinks[auxLink.Key] = auxLink.Value;
            }

            var manifestPath = Path.Combine(majorVersionDir, "manifest.json");
            Support? support = null;
            if (File.Exists(manifestPath))
            {
                Console.WriteLine($"Processing manifest file: {manifestPath}");
                Stream manifestStream = File.OpenRead(manifestPath);
                ReleaseManifest manifest = await Hal.GetMajorReleasesIndex(manifestStream) ?? throw new InvalidOperationException($"Failed to read manifest from {manifestPath}");
                support = new Support(manifest.ReleaseType, manifest.SupportPhase, manifest.GaDate, manifest.EolDate);
            }
            else
            {
                support = new Support(summary.ReleaseType, summary.SupportPhase, summary.GaDate, summary.EolDate);
            }

            // write major version index.json if there are patch releases found
            var majorIndexPath = Path.Combine(majorVersionDir, "index.json");
            var relativeMajorIndexPath = Path.GetRelativePath(rootDir, majorIndexPath);
            var patchVersionIndex = new ReleaseVersionIndex(
                ReleaseKind.Index,
                    $"Index for {summary.MajorVersionLabel} patch releases",
                    majorVersionLinks)
            {
                Embedded = patchEntries.Count > 0 ? new ReleaseVersionIndexEmbedded(patchEntries) : null,
                Support = support
            };

            // Serialize to string first to add schema reference
            var patchIndexJson = JsonSerializer.Serialize(
                patchVersionIndex,
                ReleaseVersionIndexSerializerContext.Default.ReleaseVersionIndex);
            
            // Add schema reference
            var schemaUri = "https://raw.githubusercontent.com/richlander/core/main/release-notes/schemas/release-version-index.json";
            var updatedPatchIndexJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(patchIndexJson, schemaUri);
            
            // Write to file
            using Stream patchStream = File.Create(Path.Combine(majorVersionDir, "index.json"));
            using var writer = new StreamWriter(patchStream);
            await writer.WriteAsync(updatedPatchIndexJson ?? patchIndexJson);
            await writer.WriteAsync('\n');

            // Same links as the major version index, but with a different base directory (to force different pathing)
            var majorVersionWithinAllReleasesIndexLinks = halLinkGenerator.Generate(
                majorVersionDir,
                MainFileMappings.Values,
                (fileLink, key) => key == HalTerms.Self ? summary.MajorVersionLabel : fileLink.Title);

            // Add the major version entry to the list
            var majorEntry = new ReleaseVersionIndexEntry(
                majorVersionDirName,
                ReleaseKind.Index,
                majorVersionWithinAllReleasesIndexLinks
                )
            { Support = support };

            majorEntries.Add(majorEntry);
        }

        var rootLinks = halLinkGenerator.Generate(
            rootDir,
            MainFileMappings.Values,
            (fileLink, key) => key == HalTerms.Self ? ".NET Release" : fileLink.Title);

        Console.WriteLine($"Found {rootLinks.Count} root links in {rootDir}");

        // Create the major releases index; release-notes/index.json
        var rootIndexPath = Path.Combine(rootDir, "index.json");
        var rootIndexRelativePath = Path.GetRelativePath(rootDir, rootIndexPath);
        var title = "Index of .NET major versions";
        var majorIndex = new ReleaseVersionIndex(
                ReleaseKind.Index,
                title,
                rootLinks)
        {
            Embedded = new ReleaseVersionIndexEmbedded([.. majorEntries.OrderByDescending(e => e.Version, numericStringComparer)])
        };

        // Serialize to string first to add schema reference
        var majorIndexJson = JsonSerializer.Serialize(
            majorIndex,
            ReleaseVersionIndexSerializerContext.Default.ReleaseVersionIndex);
        
        // Add schema reference
        var rootSchemaUri = "https://raw.githubusercontent.com/richlander/core/main/release-notes/schemas/release-version-index.json";
        var updatedMajorIndexJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(majorIndexJson, rootSchemaUri);
        
        // Write the major index file
        using Stream stream = File.Create(Path.Combine(rootDir, "index.json"));
        using var rootWriter = new StreamWriter(stream);
        await rootWriter.WriteAsync(updatedMajorIndexJson ?? majorIndexJson);
        await rootWriter.WriteAsync('\n');
    }

    // Generates index containing each patch release in the major version directory
    private static List<ReleaseVersionIndexEntry> GetPatchIndexEntries(IList<PatchReleaseSummary> summaries, PathContext pathContext, ReleaseHistory? releaseHistory)
    {
        var (rootDir, urlRootDir) = pathContext;

        if (!Directory.Exists(rootDir))
        {
            throw new DirectoryNotFoundException($"Output directory does not exist: {rootDir}");
        }

        var summaryTable = summaries.ToDictionary(
            s => s.PatchVersion,
            s => s,
            StringComparer.OrdinalIgnoreCase);

        List<ReleaseVersionIndexEntry> indexEntries = [];

        foreach (var summary in summaries)
        {
            if (!summaryTable.ContainsKey(summary.PatchVersion))
            {
                continue;
            }

            var patchDir = Path.Combine(rootDir, summary.PatchVersion);

            var releaseJson = Path.Combine(patchDir, "release.json");
            if (!File.Exists(releaseJson))
            {
                continue;
            }
            var relativePath = Path.GetRelativePath(rootDir, releaseJson);
            var urlRelativePath = Path.GetRelativePath(urlRootDir ?? rootDir, releaseJson);
            var links = new Dictionary<string, HalLink>
                {
                    { HalTerms.Self, new HalLink(IndexHelpers.GetProdPath(urlRelativePath))
                        {
                            Relative = relativePath,
                            Title = $"{summary.PatchVersion} Release Information",
                            Type = MediaType.Json
                        }
                    }
                };

            // Add CVE links and records if available
            IReadOnlyList<CveRecordSummary>? cveRecords = null;
            if (releaseHistory != null)
            {
                var patchYear = summary.ReleaseDate.Year.ToString();
                var patchMonth = summary.ReleaseDate.Month.ToString("D2");
                var patchDay = summary.ReleaseDate.Day.ToString("D2");

                if (releaseHistory.Years.TryGetValue(patchYear, out var year) &&
                    year.Months.TryGetValue(patchMonth, out var month) &&
                    month.Days.TryGetValue(patchDay, out var day) &&
                    !string.IsNullOrEmpty(day.CveJson))
                {
                    // Add CVE JSON link
                    var cveJsonRelativePath = $"history/{day.CveJson}";
                    links["cve-json"] = new HalLink(IndexHelpers.GetProdPath(cveJsonRelativePath))
                    {
                        Relative = cveJsonRelativePath,
                        Title = "CVE Information (JSON)",
                        Type = MediaType.Json
                    };

                    // Add CVE Markdown link
                    var cveMdPath = day.CveJson.Replace(".json", ".md");
                    var cveMdRelativePath = $"history/{cveMdPath}";
                    links["cve-markdown"] = new HalLink(IndexHelpers.GetGitHubPath(cveMdRelativePath))
                    {
                        Relative = cveMdRelativePath,
                        Title = "CVE Information (Markdown)",
                        Type = MediaType.Markdown
                    };
                }
            }

            // Add CVE records from the summary object
            if (summary.CveList?.Count > 0)
            {
                cveRecords = summary.CveList.Select(cve => new CveRecordSummary(cve.CveId, $"CVE {cve.CveId}")
                {
                    Href = cve.CveUrl
                }).ToList();
            }

            var indexEntry = new ReleaseVersionIndexEntry(summary.PatchVersion, ReleaseKind.PatchRelease, links)
            {
                CveRecords = cveRecords
            };
            indexEntries.Add(indexEntry);
        }

        return indexEntries;
    }
}
