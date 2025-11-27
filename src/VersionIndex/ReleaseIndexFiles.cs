using System.Globalization;
using System.Text.Json;
using DotnetRelease;
using DotnetRelease.Security;
using DotnetRelease.Graph;
using DotnetRelease.Summary;
using JsonSchemaInjector;

namespace VersionIndex;

public class ReleaseIndexFiles
{
    private static int _skippedFilesCount = 0;
    
    public static int SkippedFilesCount => _skippedFilesCount;
    
    public static void ResetSkippedFilesCount() => _skippedFilesCount = 0;
    
    private static UsageLinks? CreateUsageLinks(Dictionary<string, HalLink>? usageLinks = null)
    {
        if (usageLinks == null || usageLinks.Count == 0)
        {
            return null;
        }
        
        return new UsageLinks
        {
            Links = usageLinks
        };
    }
    
    // Glossary terms to exclude from VersionIndex (CVE-related terms belong in timeline)
    private static readonly string[] _excludedGlossaryTerms = ["cve", "cvss"];

    private static (Dictionary<string, HalLink> remainingLinks, Dictionary<string, HalLink>? usageLinks) ExtractUsageLinks(Dictionary<string, HalLink> allLinks)
    {
        var usageLinks = new Dictionary<string, HalLink>();
        var remainingLinks = new Dictionary<string, HalLink>();

        foreach (var (key, link) in allLinks)
        {
            // Check if this is a usage-related link
            if (key.StartsWith("usage") || key.StartsWith("glossary") || key.StartsWith("quick-reference"))
            {
                usageLinks[key] = link;
            }
            else
            {
                remainingLinks[key] = link;
            }
        }

        return (remainingLinks, usageLinks.Count > 0 ? usageLinks : null);
    }

    public static readonly OrderedDictionary<string, FileLink> MainFileMappings = new()
    {
        {FileNames.Index, new FileLink(FileNames.Index, LinkTitles.DotNetReleaseIndex, LinkStyle.Prod) },
        {"../llms/README.md", new FileLink("../llms/README.md", LinkTitles.UsageGuide, LinkStyle.Prod | LinkStyle.GitHub) },
        {"../llms/quick-ref.md", new FileLink("../llms/quick-ref.md", LinkTitles.QuickReference, LinkStyle.Prod | LinkStyle.GitHub) },
        {"../llms/glossary.md", new FileLink("../llms/glossary.md", LinkTitles.Glossary, LinkStyle.Prod | LinkStyle.GitHub) },
        {$"{FileNames.Directories.Timeline}/{FileNames.Index}", new FileLink($"{FileNames.Directories.Timeline}/{FileNames.Index}", IndexTitles.TimelineIndexLink, LinkStyle.Prod) },
        {"support.md", new FileLink("support.md", LinkTitles.SupportPolicy, LinkStyle.Prod | LinkStyle.GitHub) }
    };

    public static readonly OrderedDictionary<string, FileLink> PatchFileMappings = new()
    {
        {FileNames.Index, new FileLink(FileNames.Index, LinkTitles.Index, LinkStyle.Prod) },
        {FileNames.Manifest, new FileLink(FileNames.Manifest, LinkTitles.ReleaseManifest, LinkStyle.Prod) },
        {FileNames.Releases, new FileLink(FileNames.Releases, LinkTitles.CompleteReleaseInformation, LinkStyle.Prod) },
        {FileNames.Release, new FileLink(FileNames.Release, LinkTitles.Release, LinkStyle.Prod) }
    };

    public static readonly OrderedDictionary<string, FileLink> AuxFileMappings = new()
    {
        {FileNames.SupportedOs, new FileLink(FileNames.SupportedOs, LinkTitles.SupportedOSes, LinkStyle.Prod) },
        {"supported-os.md", new FileLink("supported-os.md", LinkTitles.SupportedOSes, LinkStyle.Prod | LinkStyle.GitHub) },
        {"linux-packages.json", new FileLink("linux-packages.json", LinkTitles.LinuxPackages, LinkStyle.Prod) },
        {"linux-packages.md", new FileLink("linux-packages.md", LinkTitles.LinuxPackages, LinkStyle.Prod | LinkStyle.GitHub) },
        {"README.md", new FileLink("README.md", LinkTitles.ReleaseNotes, LinkStyle.GitHub) }
    };

    private readonly List<string> _leafFiles = [FileNames.Releases, FileNames.Release, FileNames.Manifest];

    private static bool IsVersionSdkSupported(string version)
    {
        // SDK hive is only supported for .NET 8.0 and later
        if (string.IsNullOrEmpty(version) || !version.Contains('.'))
        {
            return false;
        }

        var parts = version.Split('.');
        if (parts.Length < 2 || !int.TryParse(parts[0], out var major))
        {
            return false;
        }

        return major >= 8;
    }

    // Generates index files for each major version directory and one root index file
    public static async Task GenerateAsync(List<MajorReleaseSummary> summaries, string inputDir, string outputDir)
    {
        if (!Directory.Exists(inputDir))
        {
            throw new DirectoryNotFoundException($"Input directory does not exist: {inputDir}");
        }

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // Load glossary from centralized file, excluding CVE-related terms
        var glossary = await GlossaryLoader.LoadExcludingAsync(inputDir, _excludedGlossaryTerms);

        var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
        List<MajorReleaseVersionIndexEntry> majorEntries = [];

        var summaryTable = summaries.ToDictionary(
            s => s.MajorVersion,
            s => s,
            StringComparer.OrdinalIgnoreCase);

        var urlGenerator = (string relativePath, LinkStyle style) => style == LinkStyle.Prod
            ? $"{Location.GitHubBaseUri}{relativePath}"
            : $"https://github.com/dotnet/core/blob/main/release-notes/{relativePath}";

        var halLinkGenerator = new HalLinkGenerator(inputDir, urlGenerator);

        // Look at all the major version directories
        // The presence of a releases.json file indicates this is a major version directory
        foreach (var majorVersionDir in Directory.EnumerateDirectories(inputDir))
        {
            var majorVersionDirName = Path.GetFileName(majorVersionDir);

            if (!summaryTable.TryGetValue(majorVersionDirName, out var summary))
            {
                continue;
            }

            // Generate manifest.json from _manifest.json and computed data
            var generatedManifest = await ManifestGenerator.GenerateManifestAsync(majorVersionDir, majorVersionDirName, halLinkGenerator);

            // Write the generated manifest.json
            var outputMajorVersionDir = Path.Combine(outputDir, majorVersionDirName);
            if (!Directory.Exists(outputMajorVersionDir))
            {
                Directory.CreateDirectory(outputMajorVersionDir);
            }
            var manifestPath = Path.Combine(outputMajorVersionDir, FileNames.Manifest);
            var manifestJson = JsonSerializer.Serialize(
                generatedManifest,
                ReleaseManifestSerializerContext.Default.ReleaseManifest);
            
            if (HalJsonComparer.ShouldWriteFile(manifestPath, manifestJson))
            {
                await File.WriteAllTextAsync(manifestPath, manifestJson);
            }
            else
            {
                _skippedFilesCount++;
            }

            // Extract lifecycle from generated manifest
            var lifecycle = generatedManifest.Lifecycle;
            if (lifecycle == null)
            {
                Console.WriteLine($"Warning: {majorVersionDirName} - Lifecycle is null");
            }

            // Generate base links from PatchFileMappings first  
            var majorVersionLinks = halLinkGenerator.Generate(
                majorVersionDir,
                PatchFileMappings.Values,
                (fileLink, key) => key == HalTerms.Self ? summary.MajorVersionLabel : fileLink.Title);

            // Generate patch version index; release-notes/8.0/index.json
            var patchEntries = await GetPatchIndexEntriesAsync(summaryTable[majorVersionDirName].PatchReleases, new PathContext(majorVersionDir, inputDir), lifecycle, outputDir, majorVersionDirName);

            // Generate aux links
            var auxLinks = halLinkGenerator.Generate(
                majorVersionDir,
                AuxFileMappings.Values,
                (fileLink, key) => fileLink.Title,
                includeSelf: false); // Don't create self link for aux files

            // Reorder links to follow spec: HAL+JSON first, then JSON, then markdown
            var orderedMajorVersionLinks = new Dictionary<string, HalLink>();
            
            // 1. Add HAL+JSON links from base mappings first
            foreach (var link in majorVersionLinks.Where(kvp => kvp.Value.Type == MediaType.HalJson))
            {
                orderedMajorVersionLinks[link.Key] = link.Value;
            }

            // 2. Add SDK links for supported versions (8.0+) - these are HAL+JSON
            if (IsVersionSdkSupported(majorVersionDirName))
            {
                var sdkIndexPath = Path.Combine(majorVersionDir, FileNames.Directories.Sdk, FileNames.Index);
                var relativeSdkIndexPath = Path.GetRelativePath(outputDir, sdkIndexPath);
                var pathValue = "/" + relativeSdkIndexPath.Replace("\\", "/");
                orderedMajorVersionLinks[LinkRelations.SdkIndex] = new HalLink($"{Location.GitHubBaseUri}{relativeSdkIndexPath}")
                {
                    Path = pathValue,
                    Title = $".NET SDK {majorVersionDirName} Release Information",
                    Type = MediaType.HalJson
                };
            }

            // 3. Collect release timeline years (used for _embedded.years later)
            // Get unique years from patch releases for this major version
            var releaseYears = summary.PatchReleases
                .Select(p => p.ReleaseDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            // 4. Add JSON-only links from base mappings
            foreach (var link in majorVersionLinks.Where(kvp => kvp.Value.Type == MediaType.Json))
            {
                orderedMajorVersionLinks[link.Key] = link.Value;
            }

            // 5. Add JSON-only links from aux mappings
            foreach (var link in auxLinks.Where(kvp => kvp.Value.Type == MediaType.Json))
            {
                orderedMajorVersionLinks[link.Key] = link.Value;
            }

            // 6. Add markdown links from aux mappings
            foreach (var link in auxLinks.Where(kvp => kvp.Value.Type == MediaType.Markdown))
            {
                orderedMajorVersionLinks[link.Key] = link.Value;
            }

            majorVersionLinks = orderedMajorVersionLinks;

            // Extract usage links from majorVersionLinks (we don't include them at major version level)
            var (remainingMajorVersionLinks, _) = ExtractUsageLinks(majorVersionLinks);

            // write major version index.json if there are patch releases found
            var majorIndexPath = Path.Combine(outputMajorVersionDir, FileNames.Index);
            var relativeMajorIndexPath = Path.GetRelativePath(inputDir, Path.Combine(majorVersionDir, FileNames.Index));

            // Collect all CVE IDs for this major version
            var allCveIds = patchEntries
                .Where(e => e.CveRecords?.Count > 0)
                .SelectMany(e => e.CveRecords!)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            // Determine latest and latest-security
            // Patches are ordered latest first, so first entry is latest
            var latestPatch = patchEntries.FirstOrDefault();

            // Get the latest patch version for the description (use latestPatch which handles semver correctly)
            var latestPatchVersion = latestPatch?.Version ?? summary.PatchReleases.Select(p => p.PatchVersion).Max(numericStringComparer);
            var patchDescription = $".NET {majorVersionDirName} (latest: {latestPatchVersion}); {Location.CacheFriendlyNote}";
            var latestSecurityPatch = patchEntries.FirstOrDefault(e => e.CveRecords?.Count > 0);
            
            // Add latest and latest-security links if available
            if (latestPatch != null)
            {
                var latestPatchIndexPath = $"{majorVersionDirName}/{latestPatch.Version}/{FileNames.Index}";
                remainingMajorVersionLinks["latest"] = new HalLink($"{Location.GitHubBaseUri}{latestPatchIndexPath}")
                {
                    Path = $"/{latestPatchIndexPath}",
                    Title = $"Latest patch release ({latestPatch.Version})",
                    Type = MediaType.HalJson
                };
            }
            
            if (latestSecurityPatch != null)
            {
                var latestSecurityPatchIndexPath = $"{majorVersionDirName}/{latestSecurityPatch.Version}/{FileNames.Index}";
                remainingMajorVersionLinks["latest-security"] = new HalLink($"{Location.GitHubBaseUri}{latestSecurityPatchIndexPath}")
                {
                    Path = $"/{latestSecurityPatchIndexPath}",
                    Title = $"Latest security patch ({latestSecurityPatch.Version})",
                    Type = MediaType.HalJson
                };
            }
            
            // Build years array for embedded data
            List<TimelineYear>? yearsEmbedded = null;
            if (releaseYears.Count > 0)
            {
                yearsEmbedded = releaseYears.Select(year =>
                {
                    var yearHistoryPath = $"{FileNames.Directories.Timeline}/{year}/{FileNames.Index}";
                    var pathValue = "/" + yearHistoryPath;
                    var yearLinks = new Dictionary<string, HalLink>
                    {
                        [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{yearHistoryPath}")
                        {
                            Path = pathValue,
                            Title = $".NET Release Timeline {year} (chronological)",
                            Type = MediaType.HalJson
                        }
                    };
                    return new TimelineYear(year.ToString(), yearLinks);
                }).ToList();
            }
            
            var patchVersionIndex = new PatchReleaseVersionIndex(
                ReleaseKind.MajorVersionIndex,
                $".NET {summary.MajorVersionLabel.Replace(".NET ", string.Empty)} Patch Release Index",
                patchDescription)
            {
                Latest = latestPatch?.Version,
                LatestSecurity = latestSecurityPatch?.Version,
                Lifecycle = lifecycle,
                Links = remainingMajorVersionLinks,
                Embedded = patchEntries.Count > 0 || yearsEmbedded != null || allCveIds.Count > 0 ? new PatchReleaseVersionIndexEmbedded(
                    patchEntries.Select(e => new PatchReleaseVersionIndexEntry(
                        e.Version,
                        e.Lifecycle?.GaDate,
                        e.CveRecords?.Count > 0,
                        e.Lifecycle?.Phase,
                        e.Links)
                    {
                        CveRecords = e.CveRecords
                    }).ToList())
                {
                    Years = yearsEmbedded,
                    CveRecords = allCveIds.Count > 0 ? allCveIds : null
                } : null,
                Metadata = new GenerationMetadata("1.0", DateTimeOffset.UtcNow, "VersionIndex")
            };

            // Serialize to string first to add schema reference
            var patchIndexJson = JsonSerializer.Serialize(
                patchVersionIndex,
                ReleaseVersionIndexSerializerContext.Default.PatchReleaseVersionIndex);

            // Add schema reference
            var schemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.ReleaseVersionIndex}";
            var updatedPatchIndexJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(patchIndexJson, schemaUri);

            // Write to file
            var patchIndexPath = Path.Combine(outputMajorVersionDir, FileNames.Index);
            var finalPatchIndexJson = (updatedPatchIndexJson ?? patchIndexJson) + '\n';
            
            if (HalJsonComparer.ShouldWriteFile(patchIndexPath, finalPatchIndexJson))
            {
                using Stream patchStream = File.Create(patchIndexPath);
                using var writer = new StreamWriter(patchStream);
                await writer.WriteAsync(finalPatchIndexJson);
            }
            else
            {
                _skippedFilesCount++;
            }

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
                
                // Set dates based on version number - actual dates would come from _manifest.json
                var currentYear = DateTimeOffset.UtcNow.Year;
                DateTimeOffset releaseDate;
                
                // Set realistic release dates based on version number
                int majorVersion;
                if (int.TryParse(majorVersionDirName.Split('.')[0], out majorVersion))
                {
                    switch (majorVersion)
                    {
                        case 10: // Future .NET 10 (Nov 2026)
                            releaseDate = new DateTimeOffset(currentYear + 1, 11, 14, 0, 0, 0, TimeSpan.Zero);
                            break;
                        case 9: // .NET 9 (Nov 2024)
                            releaseDate = new DateTimeOffset(currentYear - 1, 11, 14, 0, 0, 0, TimeSpan.Zero);
                            break;
                        case 8: // .NET 8 (Nov 2023)
                            releaseDate = new DateTimeOffset(currentYear - 2, 11, 14, 0, 0, 0, TimeSpan.Zero);
                            break;
                        case 7: // .NET 7 (Nov 2022)
                            releaseDate = new DateTimeOffset(currentYear - 3, 11, 14, 0, 0, 0, TimeSpan.Zero);
                            break;
                        case 6: // .NET 6 (Nov 2021)
                            releaseDate = new DateTimeOffset(currentYear - 4, 11, 14, 0, 0, 0, TimeSpan.Zero);
                            break;
                        default: // Older versions
                            releaseDate = new DateTimeOffset(currentYear - 5, 11, 14, 0, 0, 0, TimeSpan.Zero);
                            break;
                    }
                }
                else
                {
                    // Default to last year if parsing fails
                    releaseDate = new DateTimeOffset(currentYear - 1, 11, 14, 0, 0, 0, TimeSpan.Zero);
                }
                
                var eolDate = releaseDate.AddYears(isEven ? 3 : 1).AddMonths(isEven ? 0 : 6); // 3 years for LTS, 18 months for STS

                // Set phase based on whether the release date is in the future
                var phase = releaseDate > DateTimeOffset.UtcNow ? SupportPhase.Preview : SupportPhase.Active;

                lifecycle = new Lifecycle(releaseType, phase, releaseDate, eolDate);
            }

            // Set supported flag
            lifecycle.Supported = ReleaseStability.IsSupported(lifecycle);

            // Major version entries use full lifecycle (not simplified)
            var majorEntry = new MajorReleaseVersionIndexEntry(
                majorVersionDirName,
                majorVersionWithinAllReleasesIndexLinks)
            {
                Lifecycle = lifecycle
            };

            majorEntries.Add(majorEntry);
        }

        // Generate base links from MainFileMappings first
        var rootLinks = halLinkGenerator.Generate(
            inputDir,
            MainFileMappings.Values,
            (fileLink, key) => key == HalTerms.Self ? IndexTitles.VersionIndexLink : fileLink.Title);

        // Find latest stable release and latest LTS release (used for both links and properties)
        MajorReleaseVersionIndexEntry? latestRelease = null;
        MajorReleaseVersionIndexEntry? latestLtsRelease = null;

        // Insert dynamic HAL+JSON links after release-history-index but before markdown files
        if (majorEntries.Count > 0)
        {
            // Create a new ordered dictionary to maintain proper ordering
            var orderedRootLinks = new Dictionary<string, HalLink>();
            
            // Add HAL+JSON links first
            foreach (var link in rootLinks.Where(kvp => kvp.Value.Type == MediaType.HalJson))
            {
                orderedRootLinks[link.Key] = link.Value;
            }

            // Find latest stable and supported release
            // Uses shared ReleaseStability methods to ensure consistent logic across tools
            var releaseData = summaries.Select(s => (s.MajorVersion, (Lifecycle?)s.Lifecycle));
            var latestVersion = ReleaseStability.FindLatestVersion(releaseData, numericStringComparer);
            var latestLtsVersion = ReleaseStability.FindLatestLtsVersion(releaseData, numericStringComparer);

            latestRelease = latestVersion != null
                ? majorEntries.FirstOrDefault(e => e.Version == latestVersion)
                : null;

            if (latestRelease != null)
            {
                orderedRootLinks["latest"] = new HalLink($"{Location.GitHubBaseUri}{latestRelease.Version}/{FileNames.Index}")
                {
                    Path = $"/{latestRelease.Version}/{FileNames.Index}",
                    Title = $"Latest .NET release (.NET {latestRelease.Version})",
                    Type = MediaType.HalJson
                };
            }

            // Find latest stable LTS release (uses lifecycle.ReleaseType, not version number heuristics)
            latestLtsRelease = latestLtsVersion != null
                ? majorEntries.FirstOrDefault(e => e.Version == latestLtsVersion)
                : null;
                
            if (latestLtsRelease != null)
            {
                orderedRootLinks["latest-lts"] = new HalLink($"{Location.GitHubBaseUri}{latestLtsRelease.Version}/{FileNames.Index}")
                {
                    Path = $"/{latestLtsRelease.Version}/{FileNames.Index}",
                    Title = $"Latest LTS release (.NET {latestLtsRelease.Version})",
                    Type = MediaType.HalJson
                };
            }

            // Add latest-sdk link if version supports SDK (8.0+)
            if (latestRelease != null && IsVersionSdkSupported(latestRelease.Version))
            {
                orderedRootLinks["latest-sdk"] = new HalLink($"{Location.GitHubBaseUri}{latestRelease.Version}/{FileNames.Directories.Sdk}/{FileNames.Index}")
                {
                    Path = $"/{latestRelease.Version}/{FileNames.Directories.Sdk}/{FileNames.Index}",
                    Title = $"Latest .NET SDK ({latestRelease.Version})",
                    Type = MediaType.HalJson
                };
            }

            // Calculate latest year for cross-reference to timeline
            var latestYearForLink = summaries
                .SelectMany(s => s.PatchReleases.Select(p => p.ReleaseDate.Year))
                .DefaultIfEmpty(0)
                .Max();
            
            if (latestYearForLink > 0)
            {
                orderedRootLinks[LinkRelations.LatestYear] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{latestYearForLink}/{FileNames.Index}")
                {
                    Path = $"/{FileNames.Directories.Timeline}/{latestYearForLink}/{FileNames.Index}",
                    Title = $"Latest year ({latestYearForLink})",
                    Type = MediaType.HalJson
                };
            }

            // Add non-HAL+JSON links (markdown files) after
            foreach (var link in rootLinks.Where(kvp => kvp.Value.Type != MediaType.HalJson))
            {
                orderedRootLinks[link.Key] = link.Value;
            }

            rootLinks = orderedRootLinks;
        }

        Console.WriteLine($"Found {rootLinks.Count} root links in {inputDir}");

        // Create the major releases index; release-notes/index.json
        var rootIndexPath = Path.Combine(outputDir, FileNames.Index);
        var rootIndexRelativePath = Path.GetRelativePath(inputDir, Path.Combine(inputDir, FileNames.Index));

        // Get the latest major version for the description (use latestRelease which handles stability correctly)
        var latestMajorVersion = latestRelease?.Version ?? majorEntries.Select(e => e.Version).Max(numericStringComparer);
        var description = $".NET Release Index (latest: {latestMajorVersion}); {Location.CacheFriendlyNote}";
        
        // Calculate latest year from all patch releases across all major versions
        var latestYear = summaries
            .SelectMany(s => s.PatchReleases.Select(p => p.ReleaseDate.Year))
            .DefaultIfEmpty(0)
            .Max()
            .ToString();
        
        // Extract usage links from rootLinks
        var (remainingRootLinks, usageLinksForRoot) = ExtractUsageLinks(rootLinks);
        
        var majorIndex = new MajorReleaseVersionIndex(
                ReleaseKind.ReleasesIndex,
                IndexTitles.VersionIndexTitle,
                description)
        {
            Latest = latestRelease?.Version,
            LatestLts = latestLtsRelease?.Version,
            LatestYear = latestYear != "0" ? latestYear : null,
            Links = remainingRootLinks,
            Usage = CreateUsageLinks(usageLinksForRoot),
            Glossary = glossary,
            Embedded = new MajorReleaseVersionIndexEmbedded([.. majorEntries.OrderByDescending(e => e.Version, numericStringComparer)]),
            Metadata = new GenerationMetadata("1.0", DateTimeOffset.UtcNow, "VersionIndex")
        };

        // Serialize to string first to add schema reference
        var majorIndexJson = JsonSerializer.Serialize(
            majorIndex,
            ReleaseVersionIndexSerializerContext.Default.MajorReleaseVersionIndex);

        // Add schema reference
        var rootSchemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.ReleaseVersionIndex}";
        var updatedMajorIndexJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(majorIndexJson, rootSchemaUri);

        // Write the major index file
        var rootMajorIndexPath = Path.Combine(outputDir, FileNames.Index);
        var finalMajorIndexJson = (updatedMajorIndexJson ?? majorIndexJson) + '\n';
        
        if (HalJsonComparer.ShouldWriteFile(rootMajorIndexPath, finalMajorIndexJson))
        {
            using Stream stream = File.Create(rootMajorIndexPath);
            using var rootWriter = new StreamWriter(stream);
            await rootWriter.WriteAsync(finalMajorIndexJson);
        }
        else
        {
            _skippedFilesCount++;
        }

        // Generate llms.txt file from the root links
        // TODO: Add LlmsTxtGenerator dependency or move to separate tool
        // var llmsTxtContent = LlmsTxtGenerator.Generate(rootLinks);
        // Write llms.txt to repo root (parent of release-notes directory)
        // var repoRoot = Directory.GetParent(outputDir)?.FullName ?? outputDir;
        // var llmsTxtPath = Path.Combine(repoRoot, "llms.txt");
        
        // if (HalJsonComparer.ShouldWriteFile(llmsTxtPath, llmsTxtContent))
        // {
        //     await File.WriteAllTextAsync(llmsTxtPath, llmsTxtContent);
        // }
        // else
        // {
        //     _skippedFilesCount++;
        // }
    }

    // Generates index containing each patch release in the major version directory
    private static async Task<List<ReleaseVersionIndexEntry>> GetPatchIndexEntriesAsync(
        IList<PatchReleaseSummary> summaries, 
        PathContext pathContext, 
        Lifecycle? majorVersionLifecycle,
        string outputDir,
        string majorVersion)
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

            var releaseJson = Path.Combine(patchDir, FileNames.Release);
            if (!File.Exists(releaseJson))
            {
                continue;
            }
            var relativePath = Path.GetRelativePath(rootDir, releaseJson);
            var urlRelativePath = Path.GetRelativePath(urlRootDir ?? rootDir, releaseJson);
            var releaseJsonPathValue = "/" + relativePath.Replace("\\", "/");
            
            // Create links - self now points to index.json, with separate link to release.json
            var patchIndexPath = $"{majorVersion}/{summary.PatchVersion}/{FileNames.Index}";
            var links = new Dictionary<string, HalLink>
                {
                    { HalTerms.Self, new HalLink(IndexHelpers.GetProdPath(patchIndexPath))
                        {
                            Path = "/" + patchIndexPath,
                            Title = $"{summary.PatchVersion} Patch Index",
                            Type = MediaType.HalJson
                        }
                    }
                };

            // Extract CVE IDs for the patch index entry
            IReadOnlyList<string>? cveIds = null;
            if (summary.CveList?.Count > 0)
            {
                cveIds = summary.CveList.Select(cve => cve.CveId).ToList();
            }

            // Create simplified lifecycle for patch releases (per spec: only phase and release-date)
            SupportPhase patchPhase;
            DateTimeOffset patchReleaseDate;

            if (majorVersionLifecycle != null)
            {
                // Inherit phase from major version lifecycle
                patchPhase = majorVersionLifecycle.Phase;
                // Use actual patch release date from summary
                var releaseDateOnly = summary.ReleaseDate;
                patchReleaseDate = new DateTimeOffset(releaseDateOnly.Year, releaseDateOnly.Month, releaseDateOnly.Day, 0, 0, 0, TimeSpan.Zero);
            }
            else
            {
                // Fallback: determine phase and use summary release date
                var releaseDateOnly = summary.ReleaseDate;
                patchReleaseDate = new DateTimeOffset(releaseDateOnly.Year, releaseDateOnly.Month, releaseDateOnly.Day, 0, 0, 0, TimeSpan.Zero);
                
                // Set phase based on whether the release date is in the future
                patchPhase = patchReleaseDate > DateTimeOffset.UtcNow ? SupportPhase.Preview : SupportPhase.Active;
            }

            var patchLifecycle = new PatchLifecycle(patchPhase, patchReleaseDate);

            // Always generate patch detail index (for all patches, not just those with CVEs)
            await GeneratePatchDetailIndexAsync(
                patchDir, 
                outputDir,
                urlRootDir ?? rootDir,
                majorVersion,
                summary.PatchVersion, 
                patchLifecycle, 
                cveIds);

            var indexEntry = new ReleaseVersionIndexEntry(summary.PatchVersion, links)
            {
                CveRecords = cveIds,
                Lifecycle = patchLifecycle
            };
            indexEntries.Add(indexEntry);
        }

        return indexEntries;
    }

    // Generates a patch index file for a specific patch release
    private static async Task GeneratePatchDetailIndexAsync(
        string patchDir,
        string outputDir,
        string inputDir,
        string majorVersion,
        string patchVersion,
        PatchLifecycle lifecycle,
        IReadOnlyList<string>? cveIds)
    {
        // Create patch detail index
        var links = new Dictionary<string, HalLink>
        {
            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{patchVersion}/{FileNames.Index}")
            {
                Path = $"/{majorVersion}/{patchVersion}/{FileNames.Index}",
                Title = $"{patchVersion} Patch Index",
                Type = MediaType.HalJson
            },
            ["release-json"] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{patchVersion}/{FileNames.Release}")
            {
                Path = $"/{majorVersion}/{patchVersion}/{FileNames.Release}",
                Title = $"{patchVersion} Release Information",
                Type = MediaType.Json
            }
        };

        // Add link to major version index
        links[LinkRelations.MajorVersionIndex] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{FileNames.Index}")
        {
            Path = $"/{majorVersion}/{FileNames.Index}",
            Title = $".NET {majorVersion} Patch Release Index",
            Type = MediaType.HalJson
        };

        // Add SDK index link if version supports SDK (8.0+)
        if (IsVersionSdkSupported(majorVersion))
        {
            links[LinkRelations.SdkIndex] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{FileNames.Directories.Sdk}/{FileNames.Index}")
            {
                Path = $"/{majorVersion}/{FileNames.Directories.Sdk}/{FileNames.Index}",
                Title = $".NET SDK {majorVersion} Release Information",
                Type = MediaType.HalJson
            };
        }

        // Build runtime info with markdown links
        PatchRuntimeInfo? runtimeInfo = null;
        var versionMdPath = Path.Combine(patchDir, $"{patchVersion}.md");
        var readmePath = Path.Combine(patchDir, "README.md");

        if (File.Exists(versionMdPath))
        {
            var mdFileName = $"{patchVersion}.md";
            var runtimeLinks = new Dictionary<string, HalLink>
            {
                ["release-notes-markdown"] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{patchVersion}/{mdFileName}")
                {
                    Path = $"/{majorVersion}/{patchVersion}/{mdFileName}",
                    Title = $"{patchVersion} Release Notes",
                    Type = MediaType.Markdown
                },
                ["release-notes-markdown-rendered"] = new HalLink($"https://github.com/dotnet/core/blob/main/release-notes/{majorVersion}/{patchVersion}/{mdFileName}")
                {
                    Path = $"/{majorVersion}/{patchVersion}/{mdFileName}",
                    Title = $"{patchVersion} Release Notes (Rendered)",
                    Type = MediaType.Markdown
                }
            };
            runtimeInfo = new PatchRuntimeInfo(patchVersion) { Links = runtimeLinks };
        }
        else if (File.Exists(readmePath))
        {
            var runtimeLinks = new Dictionary<string, HalLink>
            {
                ["release-notes-markdown"] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{patchVersion}/README.md")
                {
                    Path = $"/{majorVersion}/{patchVersion}/README.md",
                    Title = $"{patchVersion} Release Notes",
                    Type = MediaType.Markdown
                },
                ["release-notes-markdown-rendered"] = new HalLink($"https://github.com/dotnet/core/blob/main/release-notes/{majorVersion}/{patchVersion}/README.md")
                {
                    Path = $"/{majorVersion}/{patchVersion}/README.md",
                    Title = $"{patchVersion} Release Notes (Rendered)",
                    Type = MediaType.Markdown
                }
            };
            runtimeInfo = new PatchRuntimeInfo(patchVersion) { Links = runtimeLinks };
        }
        else
        {
            // No markdown but still create runtime info with version
            runtimeInfo = new PatchRuntimeInfo(patchVersion);
        }

        // Load SDK versions from release.json and build SDK entries
        List<string>? sdkVersionsList = null;
        List<PatchSdkEntry>? sdkEntries = null;
        var releaseJsonPath = Path.Combine(patchDir, FileNames.Release);
        if (File.Exists(releaseJsonPath) && IsVersionSdkSupported(majorVersion))
        {
            try
            {
                var releaseJson = await File.ReadAllTextAsync(releaseJsonPath);
                var releaseDoc = JsonDocument.Parse(releaseJson);

                // Extract SDK versions - try release.sdks first, then fall back to sdks at root
                var sdkVersions = new List<string>();
                JsonElement? sdksElement = null;

                if (releaseDoc.RootElement.TryGetProperty("release", out var releaseElement) &&
                    releaseElement.TryGetProperty("sdks", out var nestedSdksElement))
                {
                    sdksElement = nestedSdksElement;
                }
                else if (releaseDoc.RootElement.TryGetProperty("sdks", out var rootSdksElement))
                {
                    sdksElement = rootSdksElement;
                }

                if (sdksElement.HasValue)
                {
                    foreach (var sdkElement in sdksElement.Value.EnumerateArray())
                    {
                        if (sdkElement.TryGetProperty("version", out var versionElement))
                        {
                            var version = versionElement.GetString();
                            if (!string.IsNullOrEmpty(version))
                            {
                                sdkVersions.Add(version);
                            }
                        }
                    }
                }

                if (sdkVersions.Count > 0)
                {
                    sdkVersionsList = sdkVersions;
                    sdkEntries = [];

                    // Build SDK entries with feature-band and markdown links
                    foreach (var sdkVersion in sdkVersions)
                    {
                        var sdkLinks = new Dictionary<string, HalLink>();

                        // Add feature-band link
                        var parts = sdkVersion.Split('.');
                        if (parts.Length >= 3)
                        {
                            var featureBand = $"{parts[0]}.{parts[1]}.{parts[2][0]}xx";
                            var sdkFeatureBandPath = $"{majorVersion}/sdk/sdk-{featureBand}.json";
                            sdkLinks["feature-band"] = new HalLink($"{Location.GitHubBaseUri}{sdkFeatureBandPath}")
                            {
                                Path = $"/{sdkFeatureBandPath}",
                                Title = $".NET SDK {featureBand}",
                                Type = MediaType.Json
                            };
                        }

                        // Add markdown links - SDK-specific if exists, otherwise fall back to runtime markdown
                        var sdkMdPath = Path.Combine(patchDir, $"{sdkVersion}.md");
                        if (File.Exists(sdkMdPath))
                        {
                            var sdkMdFileName = $"{sdkVersion}.md";
                            sdkLinks["release-notes-markdown"] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{patchVersion}/{sdkMdFileName}")
                            {
                                Path = $"/{majorVersion}/{patchVersion}/{sdkMdFileName}",
                                Title = $"SDK {sdkVersion} Release Notes",
                                Type = MediaType.Markdown
                            };
                            sdkLinks["release-notes-markdown-rendered"] = new HalLink($"https://github.com/dotnet/core/blob/main/release-notes/{majorVersion}/{patchVersion}/{sdkMdFileName}")
                            {
                                Path = $"/{majorVersion}/{patchVersion}/{sdkMdFileName}",
                                Title = $"SDK {sdkVersion} Release Notes (Rendered)",
                                Type = MediaType.Markdown
                            };
                        }
                        else if (runtimeInfo?.Links != null)
                        {
                            // Fall back to runtime markdown links
                            if (runtimeInfo.Links.TryGetValue("release-notes-markdown", out var runtimeMdLink))
                            {
                                sdkLinks["release-notes-markdown"] = runtimeMdLink;
                            }
                            if (runtimeInfo.Links.TryGetValue("release-notes-markdown-rendered", out var runtimeMdRenderedLink))
                            {
                                sdkLinks["release-notes-markdown-rendered"] = runtimeMdRenderedLink;
                            }
                        }

                        sdkEntries.Add(new PatchSdkEntry(sdkVersion)
                        {
                            Links = sdkLinks.Count > 0 ? sdkLinks : null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to extract SDK versions from {releaseJsonPath}: {ex.Message}");
            }
        }

        // Load CVE disclosures from timeline directory based on release date
        List<CveRecordSummary>? cveDisclosures = null;
        string? timelineCveJsonPath = null;
        string? timelineMonthIndexPath = null;
        
        if (lifecycle?.GaDate != null)
        {
            var releaseDate = lifecycle.GaDate;
            var year = releaseDate.Year.ToString("D4");
            var month = releaseDate.Month.ToString("D2");
            timelineCveJsonPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Cve}";
            timelineMonthIndexPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Index}";
            
            // Add link to timeline month index
            links[LinkRelations.MonthIndex] = new HalLink($"{Location.GitHubBaseUri}{timelineMonthIndexPath}")
            {
                Path = $"/{timelineMonthIndexPath}",
                Title = $"Release timeline index for {year}-{month}",
                Type = MediaType.HalJson
            };
            
            // Load CVE records from timeline directory
            var cveRecords = await CveHandler.CveLoader.LoadCveRecordsForReleaseDateAsync(inputDir, releaseDate);
            
            if (cveRecords != null)
            {
                // Filter by major version (e.g., "9.0")
                var filteredCveRecords = CveHandler.CveTransformer.FilterByRelease(cveRecords, majorVersion);
                
                if (filteredCveRecords?.Disclosures.Count > 0)
                {
                    // Sort disclosures by CVE ID for consistent ordering
                    var sortedDisclosures = filteredCveRecords.Disclosures.OrderBy(d => d.Id).ToList();
                    cveDisclosures = CveHandler.CveTransformer.ToSummaries(
                        new DotnetRelease.Security.CveRecords(
                            filteredCveRecords.LastUpdated,
                            filteredCveRecords.Title,
                            sortedDisclosures,
                            filteredCveRecords.Products,
                            filteredCveRecords.Packages,
                            filteredCveRecords.Commits,
                            filteredCveRecords.ProductName,
                            filteredCveRecords.ProductCves,
                            filteredCveRecords.PackageCves,
                            filteredCveRecords.ReleaseCves,
                            filteredCveRecords.CveReleases,
                            filteredCveRecords.CveCommits
                        )
                    );
                    
                    // Validate CVE data matches releases.json
                    var cveIdsFromCveJson = sortedDisclosures.Select(d => d.Id).ToList();
                    CveHandler.CveTransformer.ValidateCveData(patchVersion, cveIds, cveIdsFromCveJson, timelineCveJsonPath);
                    
                    // Add link to timeline CVE JSON
                    links[LinkRelations.CveJson] = new HalLink($"{Location.GitHubBaseUri}{timelineCveJsonPath}")
                    {
                        Path = $"/{timelineCveJsonPath}",
                        Title = LinkTitles.CveInformation,
                        Type = MediaType.Json
                    };
                }
            }
        }

        // Build embedded content
        // Extract sorted CVE IDs from disclosures (source of truth from cve.json)
        IReadOnlyList<string>? sortedCveIds = null;
        if (cveDisclosures != null && cveDisclosures.Count > 0)
        {
            sortedCveIds = cveDisclosures.Select(d => d.Id).ToList();
        }

        PatchDetailIndexEmbedded? embedded = null;
        if (runtimeInfo != null || sdkEntries != null || cveDisclosures != null || sortedCveIds != null)
        {
            embedded = new PatchDetailIndexEmbedded
            {
                Runtime = runtimeInfo,
                Sdks = sdkEntries,
                CveRecords = sortedCveIds,
                Disclosures = cveDisclosures
            };
        }

        var patchDetailIndex = new PatchDetailIndex(
            ReleaseKind.PatchVersionIndex,
            patchVersion,
            lifecycle?.GaDate,
            cveIds?.Count > 0,
            lifecycle?.Phase,
            $".NET {patchVersion} Patch Index",
            $"Patch information for .NET {patchVersion}")
        {
            RuntimeVersion = patchVersion,
            SdkVersions = sdkVersionsList,
            Links = links,
            Embedded = embedded,
            Metadata = new GenerationMetadata("1.0", DateTimeOffset.UtcNow, "VersionIndex")
        };

        // Serialize
        var patchDetailJson = JsonSerializer.Serialize(
            patchDetailIndex,
            ReleaseVersionIndexSerializerContext.Default.PatchDetailIndex);

        // Add schema reference
        var schemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.PatchDetailIndex}";
        var updatedJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(patchDetailJson, schemaUri);

        // Write to file
        var outputPatchDir = Path.Combine(outputDir, majorVersion, patchVersion);
        if (!Directory.Exists(outputPatchDir))
        {
            Directory.CreateDirectory(outputPatchDir);
        }

        var indexPath = Path.Combine(outputPatchDir, FileNames.Index);
        var finalJson = (updatedJson ?? patchDetailJson) + '\n';
        
        if (HalJsonComparer.ShouldWriteFile(indexPath, finalJson))
        {
            await File.WriteAllTextAsync(indexPath, finalJson);
        }
        else
        {
            _skippedFilesCount++;
        }
    }
}
