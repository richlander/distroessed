using System.Globalization;
using System.Text.Json;
using DotnetRelease;
using JsonSchemaInjector;

namespace UpdateIndexes;

public class ReleaseIndexFiles
{
    private static Dictionary<string, string> CreateGlossary()
    {
        return new Dictionary<string, string>
        {
            ["lts"] = "Long-Term Support – 3-year support window",
            ["sts"] = "Standard-Term Support – 18-month support window",
            ["release"] = "General Availability – Production-ready release",
            ["eol"] = "End of Life – No longer supported",
            ["preview"] = "Pre-release phase with previews and release candidates",
            ["active"] = "Full support with regular updates and security fixes"
        };
    }

    public static readonly OrderedDictionary<string, FileLink> MainFileMappings = new()
    {
        {"index.json", new FileLink("index.json", "Index", LinkStyle.Prod) },
        {"release.json", new FileLink("release.json", "Release", LinkStyle.Prod) },
        {"manifest.json", new FileLink("manifest.json", "Manifest", LinkStyle.Prod) },
        {"usage.md", new FileLink("usage.md", "Usage", LinkStyle.Prod | LinkStyle.GitHub) },
        {"terminology.md", new FileLink("terminology.md", "Terminology", LinkStyle.Prod | LinkStyle.GitHub) },
        {"history/index.json", new FileLink("history/index.json", "Historical Release and CVE Records", LinkStyle.Prod) }
    };

    public static readonly OrderedDictionary<string, FileLink> PatchFileMappings = new()
    {
        {"index.json", new FileLink("index.json", "Index", LinkStyle.Prod) },
        {"releases.json", new FileLink("releases.json", "Releases", LinkStyle.Prod) },
        {"release.json", new FileLink("release.json", "Release", LinkStyle.Prod) },
        {"manifest.json", new FileLink("manifest.json", "Manifest", LinkStyle.Prod) }
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
            ? $"{Location.GitHubBaseUri}{relativePath}"
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

            // Generate manifest.json from _manifest.json and computed data
            var generatedManifest = await ManifestGenerator.GenerateManifestAsync(majorVersionDir, majorVersionDirName, halLinkGenerator);

            // Write the generated manifest.json
            var manifestPath = Path.Combine(majorVersionDir, "manifest.json");
            var manifestJson = JsonSerializer.Serialize(
                generatedManifest,
                ReleaseManifestSerializerContext.Default.ReleaseManifest);
            await File.WriteAllTextAsync(manifestPath, manifestJson);

            // Extract lifecycle from generated manifest
            var lifecycle = generatedManifest.Lifecycle;
            if (lifecycle == null)
            {
                Console.WriteLine($"Warning: {majorVersionDirName} - Lifecycle is null");
            }

            var majorVersionLinks = halLinkGenerator.Generate(
                majorVersionDir,
                PatchFileMappings.Values,
                (fileLink, key) => key == HalTerms.Self ? summary.MajorVersionLabel : fileLink.Title);

            // Generate patch version index; release-notes/8.0/index.json
            var patchEntries = GetPatchIndexEntries(summaryTable[majorVersionDirName].PatchReleases, new PathContext(majorVersionDir, rootDir), releaseHistory, lifecycle);

            var auxLinks = halLinkGenerator.Generate(
                majorVersionDir,
                AuxFileMappings.Values,
                (fileLink, key) => fileLink.Title);

            // Merge aux links into major version links
            foreach (var auxLink in auxLinks)
            {
                majorVersionLinks[auxLink.Key] = auxLink.Value;
            }

            // write major version index.json if there are patch releases found
            var majorIndexPath = Path.Combine(majorVersionDir, "index.json");
            var relativeMajorIndexPath = Path.GetRelativePath(rootDir, majorIndexPath);

            // Calculate version range for patch releases
            var patchVersions = summary.PatchReleases.Select(p => p.PatchVersion).ToList();
            var minPatchVersion = patchVersions.Min(numericStringComparer);
            var maxPatchVersion = patchVersions.Max(numericStringComparer);
            var patchVersionRange = $"{minPatchVersion}–{maxPatchVersion}";

            var patchDescription = $"Index of .NET versions {patchVersionRange} (latest first); {Location.CacheFriendlyNote}";
            var patchVersionIndex = new ReleaseVersionIndex(
                ReleaseKind.Index,
                $".NET {summary.MajorVersionLabel.Replace(".NET ", string.Empty)} Patch Release Index",
                patchDescription,
                majorVersionLinks)
            {
                Glossary = CreateGlossary(),
                Embedded = patchEntries.Count > 0 ? new ReleaseVersionIndexEmbedded(patchEntries) : null,
                Lifecycle = lifecycle,
                Metadata = new GenerationMetadata(DateTimeOffset.UtcNow, "UpdateIndexes")
            };

            // Serialize to string first to add schema reference
            var patchIndexJson = JsonSerializer.Serialize(
                patchVersionIndex,
                ReleaseVersionIndexSerializerContext.Default.ReleaseVersionIndex);

            // Add schema reference
            var schemaUri = $"{Location.GitHubBaseUri}schemas/dotnet-release-version-index.json";
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
            // Ensure we have lifecycle information, even if we need to create it with defaults
            if (lifecycle == null)
            {
                // Create a default lifecycle with reasonable values based on version
                var isEven = int.TryParse(majorVersionDirName.Split('.')[0], out int versionNumber) && versionNumber % 2 == 0;
                var releaseType = isEven ? ReleaseType.LTS : ReleaseType.STS;
                var phase = SupportPhase.Preview;

                // Set dates based on version number - actual dates would come from _manifest.json
                var currentYear = DateTimeOffset.UtcNow.Year;
                var releaseDate = new DateTimeOffset(currentYear, 11, 14, 0, 0, 0, TimeSpan.Zero); // Default to November release
                var eolDate = releaseDate.AddYears(isEven ? 3 : 1).AddMonths(isEven ? 0 : 6); // 3 years for LTS, 18 months for STS

                lifecycle = new Lifecycle(releaseType, phase, releaseDate, eolDate);
            }

            // Set supported flag
            lifecycle.Supported = ReleaseStability.IsSupported(lifecycle);

            var majorEntry = new ReleaseVersionIndexEntry(
                majorVersionDirName,
                ReleaseKind.Index,
                majorVersionWithinAllReleasesIndexLinks
                )
            {
                Lifecycle = lifecycle
            };

            majorEntries.Add(majorEntry);
        }

        var rootLinks = halLinkGenerator.Generate(
            rootDir,
            MainFileMappings.Values,
            (fileLink, key) => key == HalTerms.Self ? ".NET Release" : fileLink.Title);

        // Add latest and latest-lts links if we have entries
        if (majorEntries.Count > 0)
        {
            // Find latest stable release (Active, Maintenance, or GoLive)
            var latestRelease = majorEntries
                .Where(e => ReleaseStability.IsStable(e.Lifecycle))
                .OrderByDescending(e => e.Version, numericStringComparer)
                .FirstOrDefault();

            if (latestRelease != null)
            {
                rootLinks["latest"] = new HalLink($"{Location.GitHubBaseUri}{latestRelease.Version}/index.json")
                {
                    Title = $".NET {latestRelease.Version}",
                    Type = MediaType.HalJson
                };
            }

            // Find latest stable LTS release
            var latestLtsRelease = majorEntries
                .Where(e => e.Lifecycle?.ReleaseType == ReleaseType.LTS && ReleaseStability.IsStable(e.Lifecycle))
                .OrderByDescending(e => e.Version, numericStringComparer)
                .FirstOrDefault();

            if (latestLtsRelease != null)
            {
                rootLinks["latest-lts"] = new HalLink($"{Location.GitHubBaseUri}{latestLtsRelease.Version}/index.json")
                {
                    Title = $".NET {latestLtsRelease.Version} (LTS)",
                    Type = MediaType.HalJson
                };
            }
        }

        Console.WriteLine($"Found {rootLinks.Count} root links in {rootDir}");

        // Create the major releases index; release-notes/index.json
        var rootIndexPath = Path.Combine(rootDir, "index.json");
        var rootIndexRelativePath = Path.GetRelativePath(rootDir, rootIndexPath);

        // Calculate version range for description
        var majorVersions = majorEntries.Select(e => e.Version).ToList();
        var minMajorVersion = majorVersions.Min(numericStringComparer);
        var maxMajorVersion = majorVersions.Max(numericStringComparer);
        var versionRange = $"{minMajorVersion}–{maxMajorVersion}";

        var description = $"Index of .NET versions {versionRange} (latest first); {Location.CacheFriendlyNote}";
        // Create a root-level lifecycle
        var rootLifecycle = new Lifecycle(
            ReleaseType.LTS, // Default to LTS for root
            SupportPhase.Active, // Always active
            DateTimeOffset.UtcNow.AddYears(-1), // Default to 1 year ago release
            DateTimeOffset.UtcNow.AddYears(10)) // Default to 10 years from now EOL
        {
            Supported = true // Always supported
        };

        var majorIndex = new ReleaseVersionIndex(
                ReleaseKind.Index,
                ".NET Release Version Index",
                description,
                rootLinks)
        {
            Glossary = CreateGlossary(),
            Embedded = new ReleaseVersionIndexEmbedded([.. majorEntries.OrderByDescending(e => e.Version, numericStringComparer)]),
            Lifecycle = rootLifecycle,
            Metadata = new GenerationMetadata(DateTimeOffset.UtcNow, "UpdateIndexes")
        };

        // Serialize to string first to add schema reference
        var majorIndexJson = JsonSerializer.Serialize(
            majorIndex,
            ReleaseVersionIndexSerializerContext.Default.ReleaseVersionIndex);

        // Add schema reference
        var rootSchemaUri = $"{Location.GitHubBaseUri}schemas/dotnet-release-version-index.json";
        var updatedMajorIndexJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(majorIndexJson, rootSchemaUri);

        // Write the major index file
        using Stream stream = File.Create(Path.Combine(rootDir, "index.json"));
        using var rootWriter = new StreamWriter(stream);
        await rootWriter.WriteAsync(updatedMajorIndexJson ?? majorIndexJson);
        await rootWriter.WriteAsync('\n');
    }

    // Generates index containing each patch release in the major version directory
    private static List<ReleaseVersionIndexEntry> GetPatchIndexEntries(IList<PatchReleaseSummary> summaries, PathContext pathContext, ReleaseHistory? releaseHistory, Lifecycle? majorVersionLifecycle)
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

            // Create a copy of the major version lifecycle for each patch
            Lifecycle patchLifecycle;
            if (majorVersionLifecycle != null)
            {
                patchLifecycle = majorVersionLifecycle with { };
                patchLifecycle.Supported = ReleaseStability.IsSupported(majorVersionLifecycle);
            }
            else
            {
                // Create a default lifecycle if the major version lifecycle is missing
                var isEven = int.TryParse(summary.PatchVersion.Split('.')[0], out int versionNumber) && versionNumber % 2 == 0;
                var releaseType = isEven ? ReleaseType.LTS : ReleaseType.STS;
                var phase = SupportPhase.Active; // Assume active for patch releases

                // Set dates based on release information
                var releaseDateOnly = summary.ReleaseDate;
                var releaseDate = new DateTimeOffset(releaseDateOnly.Year, releaseDateOnly.Month, releaseDateOnly.Day, 0, 0, 0, TimeSpan.Zero);
                var eolDate = releaseDate.AddYears(isEven ? 3 : 1).AddMonths(isEven ? 0 : 6); // 3 years for LTS, 18 months for STS

                patchLifecycle = new Lifecycle(releaseType, phase, releaseDate, eolDate);
                patchLifecycle.Supported = ReleaseStability.IsSupported(patchLifecycle);
            }

            var indexEntry = new ReleaseVersionIndexEntry(summary.PatchVersion, ReleaseKind.PatchRelease, links)
            {
                CveRecords = cveRecords,
                Lifecycle = patchLifecycle
            };
            indexEntries.Add(indexEntry);
        }

        return indexEntries;
    }
}
