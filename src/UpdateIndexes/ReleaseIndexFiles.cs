using System.Globalization;
using System.Text.Json;
using DotnetRelease;
using JsonSchemaInjector;

namespace UpdateIndexes;

public class ReleaseIndexFiles
{
    private static int _skippedFilesCount = 0;
    
    public static int SkippedFilesCount => _skippedFilesCount;
    
    public static void ResetSkippedFilesCount() => _skippedFilesCount = 0;
    
    private static GlossaryWithLinks CreateGlossary(Dictionary<string, HalLink>? glossaryLinks = null)
    {
        return new GlossaryWithLinks
        {
            Links = glossaryLinks,
            Terms = new Dictionary<string, string>
            {
                ["lts"] = "Long-Term Support – 3-year support window",
                ["sts"] = "Standard-Term Support – 18-month support window",
                ["release"] = "General Availability – Production-ready release",
                ["eol"] = "End of Life – No longer supported",
                ["preview"] = "Pre-release phase with previews and release candidates",
                ["active"] = "Full support with regular updates and security fixes"
            }
        };
    }

    private static (Dictionary<string, HalLink> remainingLinks, Dictionary<string, HalLink>? glossaryLinks) ExtractGlossaryLinks(Dictionary<string, HalLink> allLinks)
    {
        var glossaryLinks = new Dictionary<string, HalLink>();
        var remainingLinks = new Dictionary<string, HalLink>();

        foreach (var (key, link) in allLinks)
        {
            // Check if this is a glossary-related link
            if (key.StartsWith("glossary"))
            {
                glossaryLinks[key] = link;
            }
            else
            {
                remainingLinks[key] = link;
            }
        }

        return (remainingLinks, glossaryLinks.Count > 0 ? glossaryLinks : null);
    }

    public static readonly OrderedDictionary<string, FileLink> MainFileMappings = new()
    {
        {"index.json", new FileLink("index.json", ".NET Release Index", LinkStyle.Prod) },
        {"usage.md", new FileLink("usage.md", "Usage Guide", LinkStyle.Prod | LinkStyle.GitHub) },
        {"glossary.md", new FileLink("glossary.md", "Glossary", LinkStyle.Prod | LinkStyle.GitHub) },
        {"archives/index.json", new FileLink("archives/index.json", "Security Advisories", LinkStyle.Prod) },
        {"support.md", new FileLink("support.md", "Support Policy", LinkStyle.Prod | LinkStyle.GitHub) }
    };

    public static readonly OrderedDictionary<string, FileLink> PatchFileMappings = new()
    {
        {"index.json", new FileLink("index.json", "Index", LinkStyle.Prod) },
        {"manifest.json", new FileLink("manifest.json", "Release manifest", LinkStyle.Prod) },
        {"releases.json", new FileLink("releases.json", "Complete (large file) release information for all patch releases", LinkStyle.Prod) },
        {"release.json", new FileLink("release.json", "Release", LinkStyle.Prod) }
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
    public static async Task GenerateAsync(List<MajorReleaseSummary> summaries, string inputDir, string outputDir, ReleaseHistory? releaseHistory = null)
    {
        if (!Directory.Exists(inputDir))
        {
            throw new DirectoryNotFoundException($"Input directory does not exist: {inputDir}");
        }

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

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
            var manifestPath = Path.Combine(outputMajorVersionDir, "manifest.json");
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
            var patchEntries = GetPatchIndexEntries(summaryTable[majorVersionDirName].PatchReleases, new PathContext(majorVersionDir, inputDir), releaseHistory, lifecycle);

            // Generate aux links
            var auxLinks = halLinkGenerator.Generate(
                majorVersionDir,
                AuxFileMappings.Values,
                (fileLink, key) => fileLink.Title);

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
                var sdkIndexPath = Path.Combine(majorVersionDir, "sdk", "index.json");
                var relativeSdkIndexPath = Path.GetRelativePath(outputDir, sdkIndexPath);
                orderedMajorVersionLinks["sdk-index"] = new HalLink($"{Location.GitHubBaseUri}{relativeSdkIndexPath}")
                {
                    Relative = relativeSdkIndexPath,
                    Title = $".NET SDK {majorVersionDirName} Release Information",
                    Type = MediaType.HalJson
                };
            }

            // 3. Add JSON-only links from base mappings
            foreach (var link in majorVersionLinks.Where(kvp => kvp.Value.Type == MediaType.Json))
            {
                orderedMajorVersionLinks[link.Key] = link.Value;
            }

            // 4. Add JSON-only links from aux mappings
            foreach (var link in auxLinks.Where(kvp => kvp.Value.Type == MediaType.Json))
            {
                orderedMajorVersionLinks[link.Key] = link.Value;
            }

            // 5. Add markdown links from aux mappings
            foreach (var link in auxLinks.Where(kvp => kvp.Value.Type == MediaType.Markdown))
            {
                orderedMajorVersionLinks[link.Key] = link.Value;
            }

            majorVersionLinks = orderedMajorVersionLinks;

            // Extract glossary links from majorVersionLinks
            var (remainingMajorVersionLinks, glossaryLinksForPatch) = ExtractGlossaryLinks(majorVersionLinks);

            // write major version index.json if there are patch releases found
            var majorIndexPath = Path.Combine(outputMajorVersionDir, "index.json");
            var relativeMajorIndexPath = Path.GetRelativePath(inputDir, Path.Combine(majorVersionDir, "index.json"));

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
                remainingMajorVersionLinks)
            {
                Glossary = CreateGlossary(glossaryLinksForPatch),
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
            var patchIndexPath = Path.Combine(outputMajorVersionDir, "index.json");
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
                ReleaseKind.Index,
                majorVersionWithinAllReleasesIndexLinks
                )
            {
                Lifecycle = lifecycle
            };

            majorEntries.Add(majorEntry);
        }

        // Generate base links from MainFileMappings first
        var rootLinks = halLinkGenerator.Generate(
            inputDir,
            MainFileMappings.Values,
            (fileLink, key) => key == HalTerms.Self ? ".NET Release" : fileLink.Title);

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
            var latestRelease = majorEntries
                .Where(e => e.Lifecycle != null && ReleaseStability.IsStable(e.Lifecycle.phase))
                .OrderByDescending(e => e.Version, numericStringComparer)
                .FirstOrDefault();
            
            if (latestRelease != null)
            {
                orderedRootLinks["newest-release"] = new HalLink($"{Location.GitHubBaseUri}{latestRelease.Version}/index.json")
                {
                    Relative = $"{latestRelease.Version}/index.json",
                    Title = $"Latest .NET release (.NET {latestRelease.Version})",
                    Type = MediaType.HalJson
                };
            }

            // Find latest stable LTS release (even major versions are LTS)
            var latestLtsRelease = majorEntries
                .Where(e => e.Lifecycle != null && 
                           ReleaseStability.IsStable(e.Lifecycle.phase) &&
                           int.TryParse(e.Version.Split('.')[0], out int majorVersion) && 
                           majorVersion % 2 == 0)
                .OrderByDescending(e => e.Version, numericStringComparer)
                .FirstOrDefault();
                
            if (latestLtsRelease != null)
            {
                orderedRootLinks["lts-release"] = new HalLink($"{Location.GitHubBaseUri}{latestLtsRelease.Version}/index.json")
                {
                    Relative = $"{latestLtsRelease.Version}/index.json",
                    Title = $"LTS release (.NET {latestLtsRelease.Version})",
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
        var rootIndexPath = Path.Combine(outputDir, "index.json");
        var rootIndexRelativePath = Path.GetRelativePath(inputDir, Path.Combine(inputDir, "index.json"));

        // Calculate version range for description
        var majorVersions = majorEntries.Select(e => e.Version).ToList();
        var minMajorVersion = majorVersions.Min(numericStringComparer);
        var maxMajorVersion = majorVersions.Max(numericStringComparer);
        var versionRange = $"{minMajorVersion}–{maxMajorVersion}";

        var description = $"Index of .NET versions {versionRange} (latest first); {Location.CacheFriendlyNote}";
        
        // Extract glossary links from rootLinks
        var (remainingRootLinks, glossaryLinksForRoot) = ExtractGlossaryLinks(rootLinks);
        
        var majorIndex = new MajorReleaseVersionIndex(
                ReleaseKind.Index,
                ".NET Release Version Index",
                description,
                remainingRootLinks)
        {
            Glossary = CreateGlossary(glossaryLinksForRoot),
            Embedded = new MajorReleaseVersionIndexEmbedded([.. majorEntries.OrderByDescending(e => e.Version, numericStringComparer)]),
            Metadata = new GenerationMetadata(DateTimeOffset.UtcNow, "UpdateIndexes")
        };

        // Serialize to string first to add schema reference
        var majorIndexJson = JsonSerializer.Serialize(
            majorIndex,
            ReleaseVersionIndexSerializerContext.Default.MajorReleaseVersionIndex);

        // Add schema reference
        var rootSchemaUri = $"{Location.GitHubBaseUri}schemas/dotnet-release-version-index.json";
        var updatedMajorIndexJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(majorIndexJson, rootSchemaUri);

        // Write the major index file
        var rootMajorIndexPath = Path.Combine(outputDir, "index.json");
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
        var llmsTxtContent = LlmsTxtGenerator.Generate(rootLinks);
        // Write llms.txt to repo root (parent of release-notes directory)
        var repoRoot = Directory.GetParent(outputDir)?.FullName ?? outputDir;
        var llmsTxtPath = Path.Combine(repoRoot, "llms.txt");
        
        if (HalJsonComparer.ShouldWriteFile(llmsTxtPath, llmsTxtContent))
        {
            await File.WriteAllTextAsync(llmsTxtPath, llmsTxtContent);
        }
        else
        {
            _skippedFilesCount++;
        }
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
                    var cveJsonRelativePath = $"archives/{day.CveJson}";
                    links["cve-json"] = new HalLink(IndexHelpers.GetProdPath(cveJsonRelativePath))
                    {
                        Relative = cveJsonRelativePath,
                        Title = "CVE Information (JSON)",
                        Type = MediaType.Json
                    };

                    // Add CVE Markdown link
                    var cveMdPath = day.CveJson.Replace(".json", ".md");
                    var cveMdRelativePath = $"archives/{cveMdPath}";
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

            // Create simplified lifecycle for patch releases (per spec: only phase and release-date)
            SupportPhase patchPhase;
            DateTimeOffset patchReleaseDate;

            if (majorVersionLifecycle != null)
            {
                // Inherit phase from major version lifecycle
                patchPhase = majorVersionLifecycle.phase;
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
