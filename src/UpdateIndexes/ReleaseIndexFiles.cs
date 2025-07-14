using System.Globalization;
using System.Text.Json;
using DotnetRelease;

namespace UpdateIndexes;

public class ReleaseIndexFiles
{
    public static readonly Dictionary<string, FileLink> MainFileMappings = new()
    {
        {"index.json", new FileLink("index.json", "Index", LinkStyle.Prod) },
        {"releases.json", new FileLink("releases.json", "Releases", LinkStyle.Prod) },
        {"release.json", new FileLink("release.json", "Release", LinkStyle.Prod) },
        {"manifest.json", new FileLink("manifest.json", "Manifest", LinkStyle.Prod) },
        {"usage.md", new FileLink("usage.md", "Usage", LinkStyle.Prod | LinkStyle.GitHub) },
        {"terminology.md", new FileLink("terminology.md", "Terminology", LinkStyle.Prod | LinkStyle.GitHub) }
    };

    public static readonly Dictionary<string, FileLink> AuxFileMappings = new()
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

        var numericStringComparer = StringComparer.OrdinalIgnoreCase;
        List<ReleaseIndexEntry> majorEntries = [];

        var summaryTable = summaries.ToDictionary(
            s => s.MajorVersion,
            s => s,
            StringComparer.OrdinalIgnoreCase);

        var urlGenerator = (string relativePath, LinkStyle style) => style == LinkStyle.Prod 
            ? $"https://raw.githubusercontent.com/richlander/core/main/release-notes/{relativePath}"
            : $"https://github.com/dotnet/core/blob/main/release-notes/{relativePath}";

        var halLinkGenerator = new HalLinkGenerator(rootDir, urlGenerator);

        // Process each major version directory
        foreach (var majorVersionDirName in Directory.GetDirectories(rootDir)
                     .Select(Path.GetFileName)
                     .Where(name => !string.IsNullOrEmpty(name) && summaryTable.ContainsKey(name))
                     .OrderByDescending(v => v, numericStringComparer))
        {
            var summary = summaryTable[majorVersionDirName];
            var majorVersionDir = Path.Combine(rootDir, majorVersionDirName);

            // Generate major version index entries
            var patchEntries = GetPatchIndexEntries(summaryTable[majorVersionDirName].PatchReleases, new(majorVersionDir, rootDir), releaseHistory);

            // Generate patch-level index.json files
            await GeneratePatchLevelIndexes(summary.PatchReleases, majorVersionDir, rootDir, releaseHistory);

            // Generate links for the major version index file
            var majorVersionLinks = GetMajorVersionLinks(halLinkGenerator, majorVersionDirName, majorVersionDir);

            // Create the major version index
            var majorVersionIndex = new ReleaseIndex(
                ReleaseKind.Index,
                $".NET {majorVersionDirName}",
                majorVersionLinks
            )
            {
                Schema = SchemaUrls.MajorVersionIndex,
                Embedded = new ReleaseIndexEmbedded(patchEntries),
                Support = new Support(summary.ReleaseType, summary.SupportPhase, summary.GaDate, summary.EolDate)
            };

            // Write the major version index file
            var majorVersionIndexPath = Path.Combine(majorVersionDir, "index.json");
            using var majorVersionStream = File.Create(majorVersionIndexPath);
            JsonSerializer.Serialize(
                majorVersionStream,
                majorVersionIndex,
                ReleaseIndexSerializerContext.Default.ReleaseIndex);

            // Create entry for root index
            var majorEntryLinks = GetMajorEntryLinks(halLinkGenerator, majorVersionDirName);
            var majorEntry = new ReleaseIndexEntry(summary.MajorVersion, ReleaseKind.Index, majorEntryLinks)
            {
                Support = new Support(summary.ReleaseType, summary.SupportPhase, summary.GaDate, summary.EolDate)
            };
            majorEntries.Add(majorEntry);
        }

        // Generate root index links
        var rootIndexLinks = GetRootIndexLinks(halLinkGenerator);

        // Create the root index
        var rootIndex = new ReleaseIndex(
            ReleaseKind.Index,
            "Index of .NET major versions",
            rootIndexLinks
        )
        {
            Schema = SchemaUrls.MajorVersionIndex,
            Embedded = new ReleaseIndexEmbedded([.. majorEntries.OrderByDescending(e => e.Version, numericStringComparer)])
        };

        // Write the root index file
        var rootIndexPath = Path.Combine(rootDir, "index.json");
        using var rootStream = File.Create(rootIndexPath);
        JsonSerializer.Serialize(
            rootStream,
            rootIndex,
            ReleaseIndexSerializerContext.Default.ReleaseIndex);
    }

    // NEW: Generates individual index.json files for each patch version (e.g., 8.0/8.0.1/index.json)
    private static async Task GeneratePatchLevelIndexes(IList<PatchReleaseSummary> patchReleases, string majorVersionDir, string rootDir, ReleaseHistory? releaseHistory)
    {
        var urlGenerator = (string relativePath, LinkStyle style) => style == LinkStyle.Prod 
            ? $"https://raw.githubusercontent.com/richlander/core/main/release-notes/{relativePath}"
            : $"https://github.com/dotnet/core/blob/main/release-notes/{relativePath}";

        var halLinkGenerator = new HalLinkGenerator(rootDir, urlGenerator);

        foreach (var patchSummary in patchReleases)
        {
            var patchDir = Path.Combine(majorVersionDir, patchSummary.PatchVersion);
            
            // Only create patch index if the patch directory exists
            if (!Directory.Exists(patchDir))
            {
                continue;
            }

            // Create patch-level links
            var patchLinks = new Dictionary<string, HalLink>();

            // Self link
            var patchIndexRelativePath = Path.GetRelativePath(rootDir, Path.Combine(patchDir, "index.json"));
            patchLinks[HalTerms.Self] = new HalLink(IndexHelpers.GetProdPath(patchIndexRelativePath))
            {
                Relative = patchIndexRelativePath,
                Title = $".NET {patchSummary.PatchVersion} Index",
                Type = MediaType.HalJson
            };

            // Link to major version index (parent)
            var majorIndexRelativePath = Path.GetRelativePath(rootDir, Path.Combine(majorVersionDir, "index.json"));
            patchLinks["major-version-index"] = new HalLink(IndexHelpers.GetProdPath(majorIndexRelativePath))
            {
                Relative = majorIndexRelativePath,
                Title = $".NET {patchSummary.MajorVersion} Index",
                Type = MediaType.HalJson
            };

            // Add links to available files in the patch directory
            foreach (var (fileName, fileLink) in MainFileMappings)
            {
                var filePath = Path.Combine(patchDir, fileName);
                if (File.Exists(filePath))
                {
                    var fileRelativePath = Path.GetRelativePath(rootDir, filePath);
                    var linkKey = fileName.Replace(".json", "").Replace(".md", "");
                    
                    patchLinks[linkKey] = new HalLink(
                        fileLink.Style.HasFlag(LinkStyle.GitHub) 
                            ? IndexHelpers.GetGitHubPath(fileRelativePath)
                            : IndexHelpers.GetProdPath(fileRelativePath))
                    {
                        Relative = fileRelativePath,
                        Title = fileLink.Title,
                        Type = fileName.EndsWith(".md") ? MediaType.Markdown : MediaType.Json
                    };
                }
            }

            // Add CVE information if available
            IReadOnlyList<CveRecordSummary>? cveRecords = null;
            if (releaseHistory != null)
            {
                var patchYear = patchSummary.ReleaseDate.Year.ToString();
                var patchMonth = patchSummary.ReleaseDate.Month.ToString("D2");
                var patchDay = patchSummary.ReleaseDate.Day.ToString("D2");

                if (releaseHistory.Years.TryGetValue(patchYear, out var year) &&
                    year.Months.TryGetValue(patchMonth, out var month) &&
                    month.Days.TryGetValue(patchDay, out var day) &&
                    !string.IsNullOrEmpty(day.CveJson))
                {
                    var cveJsonRelativePath = $"history/{day.CveJson}";
                    patchLinks["cve-json"] = new HalLink(IndexHelpers.GetProdPath(cveJsonRelativePath))
                    {
                        Relative = cveJsonRelativePath,
                        Title = "CVE Information (JSON)",
                        Type = MediaType.Json
                    };
                }
            }

            // Add CVE records from the summary
            if (patchSummary.CveList?.Count > 0)
            {
                cveRecords = patchSummary.CveList.Select(cve => new CveRecordSummary(cve.CveId, $"CVE {cve.CveId}")
                {
                    Href = cve.CveUrl
                }).ToList();
            }

            // Create the patch-level index
            var patchIndex = new ReleaseIndex(
                ReleaseKind.PatchRelease,
                $".NET {patchSummary.PatchVersion}",
                patchLinks
            )
            {
                Schema = SchemaUrls.MajorVersionIndex, // Note: Could use a patch-specific schema in the future
                Support = null // Patch-level support info would need to be derived from major version
            };

            // Only add embedded data if there are CVE records
            if (cveRecords?.Count > 0)
            {
                var patchEntry = new ReleaseIndexEntry(patchSummary.PatchVersion, ReleaseKind.PatchRelease, new Dictionary<string, HalLink>())
                {
                    CveRecords = cveRecords
                };
                patchIndex.Embedded = new ReleaseIndexEmbedded([patchEntry]);
            }

            // Write the patch-level index file
            var patchIndexPath = Path.Combine(patchDir, "index.json");
            using var patchStream = File.Create(patchIndexPath);
            await JsonSerializer.SerializeAsync(
                patchStream,
                patchIndex,
                ReleaseIndexSerializerContext.Default.ReleaseIndex);

            // Generate manifest.json for the patch version
            await GeneratePatchManifest(patchSummary, patchDir, rootDir, cveRecords);
        }
    }

    // NEW: Generates manifest.json files for each patch version with runtime/SDK version info
    private static async Task GeneratePatchManifest(PatchReleaseSummary patchSummary, string patchDir, string rootDir, IReadOnlyList<CveRecordSummary>? cveRecords)
    {
        // Create manifest links
        var manifestLinks = new Dictionary<string, HalLink>();

        // Self link to the manifest
        var manifestRelativePath = Path.GetRelativePath(rootDir, Path.Combine(patchDir, "manifest.json"));
        manifestLinks[HalTerms.Self] = new HalLink(IndexHelpers.GetProdPath(manifestRelativePath))
        {
            Relative = manifestRelativePath,
            Title = $".NET {patchSummary.PatchVersion} Manifest",
            Type = MediaType.Json
        };

        // Link to patch index
        var indexRelativePath = Path.GetRelativePath(rootDir, Path.Combine(patchDir, "index.json"));
        manifestLinks[HalTerms.Index] = new HalLink(IndexHelpers.GetProdPath(indexRelativePath))
        {
            Relative = indexRelativePath,
            Title = $".NET {patchSummary.PatchVersion} Index",
            Type = MediaType.HalJson
        };

        // Extract runtime and SDK information from components
        RuntimeVersionInfo? runtimeInfo = null;
        SdkVersionInfo? sdkInfo = null;

        foreach (var component in patchSummary.Components)
        {
            if (component.Name.Equals("Microsoft.NETCore.App", StringComparison.OrdinalIgnoreCase) ||
                component.Name.Equals(".NET Runtime", StringComparison.OrdinalIgnoreCase))
            {
                runtimeInfo = new RuntimeVersionInfo(component.Version, patchSummary.ReleaseDate.ToDateTime(TimeOnly.MinValue))
                {
                    BuildInfo = component.Label != component.Version ? component.Label : null
                };
            }
            else if (component.Name.Equals("Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase) ||
                     component.Name.Equals(".NET SDK", StringComparison.OrdinalIgnoreCase))
            {
                sdkInfo = new SdkVersionInfo(component.Version, patchSummary.ReleaseDate.ToDateTime(TimeOnly.MinValue))
                {
                    BuildInfo = component.Label != component.Version ? component.Label : null,
                    FeatureBand = ExtractFeatureBand(component.Version)
                };
            }
        }

        // Create the manifest
        var manifest = new ReleaseManifest(
            ReleaseKind.Manifest,
            manifestLinks,
            patchSummary.PatchVersion,
            $".NET {patchSummary.PatchVersion}",
            patchSummary.ReleaseDate.ToDateTime(TimeOnly.MinValue), // GA date is the release date for patches
            DateTime.MaxValue, // EOL date - would need to be calculated based on major version lifecycle
            ReleaseType.STS, // Default to STS; would need actual data
            SupportPhase.Active // Default to Active; would need actual data
        )
        {
            Schema = SchemaUrls.ReleaseManifest,
            Runtime = runtimeInfo,
            Sdk = sdkInfo,
            CveRecords = cveRecords
        };

        // Write the manifest file
        var manifestPath = Path.Combine(patchDir, "manifest.json");
        using var manifestStream = File.Create(manifestPath);
        await JsonSerializer.SerializeAsync(
            manifestStream,
            manifest,
            ReleaseManifestSerializerContext.Default.ReleaseManifest);
    }

    // Helper method to extract SDK feature band from version
    private static string? ExtractFeatureBand(string sdkVersion)
    {
        // SDK versions typically follow the pattern Major.Minor.FeatureBand.Patch
        // For example: 8.0.404 has feature band 4xx
        var parts = sdkVersion.Split('.');
        if (parts.Length >= 3 && int.TryParse(parts[2], out var featureBandNumber))
        {
            return $"{featureBandNumber / 100}xx";
        }
        return null;
    }

    // Generates index containing each patch release in the major version directory
    private static List<ReleaseIndexEntry> GetPatchIndexEntries(IList<PatchReleaseSummary> summaries, PathContext pathContext, ReleaseHistory? releaseHistory)
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

        List<ReleaseIndexEntry> indexEntries = [];

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

            var indexEntry = new ReleaseIndexEntry(summary.PatchVersion, ReleaseKind.PatchRelease, links)
            {
                CveRecords = cveRecords
            };
            indexEntries.Add(indexEntry);
        }

        return indexEntries;
    }

    // Generates links for the major version index file
    private static Dictionary<string, HalLink> GetMajorVersionLinks(HalLinkGenerator halLinkGenerator, string majorVersionDirName, string majorVersionDir)
    {
        var majorVersionLinks = halLinkGenerator.Generate(
            majorVersionDir,
            MainFileMappings.Values,
            (fileLink, key) => key == HalTerms.Self ? $".NET {majorVersionDirName}" : fileLink.Title);

        // Add aux links
        foreach (var auxLink in halLinkGenerator.Generate(
            majorVersionDir,
            AuxFileMappings.Values,
            (fileLink, key) => fileLink.Title))
        {
            majorVersionLinks[auxLink.Key] = auxLink.Value;
        }

        return majorVersionLinks;
    }

    // Generates links for the root index file
    private static Dictionary<string, HalLink> GetRootIndexLinks(HalLinkGenerator halLinkGenerator)
    {
        return halLinkGenerator.Generate(
            ".",
            MainFileMappings.Values,
            (fileLink, key) => key == HalTerms.Self ? ".NET Release" : fileLink.Title);
    }

    // Generates links for the major version entry in the root index file
    private static Dictionary<string, HalLink> GetMajorEntryLinks(HalLinkGenerator halLinkGenerator, string majorVersionDirName)
    {
        return halLinkGenerator.Generate(
            majorVersionDirName,
            MainFileMappings.Values,
            (fileLink, key) => key == HalTerms.Self ? $".NET {majorVersionDirName}" : fileLink.Title);
    }
}
