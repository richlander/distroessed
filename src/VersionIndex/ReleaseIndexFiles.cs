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
    public static readonly OrderedDictionary<string, FileLink> MainFileMappings = new()
    {
        {FileNames.Index, new FileLink(FileNames.Index, LinkTitles.DotNetReleaseIndex, LinkStyle.Prod) },
        {$"{FileNames.Directories.Timeline}/{FileNames.Index}", new FileLink($"{FileNames.Directories.Timeline}/{FileNames.Index}", IndexTitles.TimelineIndexLink, LinkStyle.Prod) },
    };

    // Links for major version index - lean navigation hub
    public static readonly OrderedDictionary<string, FileLink> MajorVersionFileMappings = new()
    {
        {FileNames.Index, new FileLink(FileNames.Index, LinkTitles.Index, LinkStyle.Prod) },
        {FileNames.Manifest, new FileLink(FileNames.Manifest, LinkTitles.ReleaseManifest, LinkStyle.Prod) },
    };

    // Links for manifest.json - operational/reference links
    public static readonly OrderedDictionary<string, FileLink> ManifestFileMappings = new()
    {
        {FileNames.Releases, new FileLink(FileNames.Releases, LinkTitles.CompleteReleaseInformation, LinkStyle.Prod) },
        {FileNames.SupportedOs, new FileLink(FileNames.SupportedOs, LinkTitles.SupportedOSes, LinkStyle.Prod) },
        {FileNames.OsPackages, new FileLink(FileNames.OsPackages, LinkTitles.OsPackages, LinkStyle.Prod) },
        {"linux-packages.json", new FileLink("linux-packages.json", LinkTitles.LinuxPackages, LinkStyle.Prod) },
        {"supported-os.md", new FileLink("supported-os.md", LinkTitles.SupportedOSes, LinkStyle.Prod | LinkStyle.GitHub) },
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
            await File.WriteAllTextAsync(manifestPath, manifestJson);

            // Use lifecycle from summary (canonical source from ReleaseSummaryLoader)
            var lifecycle = summary.Lifecycle;

            // Generate base links for major version index (lean navigation hub)
            var majorVersionLinks = halLinkGenerator.Generate(
                majorVersionDir,
                MajorVersionFileMappings.Values,
                (fileLink, key) => key == HalTerms.Self ? summary.MajorVersionLabel : fileLink.Title);

            // Generate patch version index; release-notes/8.0/index.json
            var patchEntries = await GetPatchIndexEntriesAsync(summaryTable[majorVersionDirName].PatchReleases, new PathContext(majorVersionDir, inputDir), lifecycle, outputDir, majorVersionDirName);

            // Collect release timeline years (used for _embedded.years later)
            // Get unique years from patch releases for this major version
            var releaseYears = summary.PatchReleases
                .Select(p => p.ReleaseDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            // Determine latest and latest-security
            // Patches are ordered latest first, so first entry is latest
            var latestPatch = patchEntries.FirstOrDefault();

            // Get the latest patch version for the description (use latestPatch which handles semver correctly)
            var latestPatchVersion = latestPatch?.Version ?? summary.PatchReleases.Select(p => p.PatchVersion).Max(numericStringComparer);
            var patchDescription = $".NET {majorVersionDirName} (latest: {latestPatchVersion})";
            var latestSecurityPatch = patchEntries.FirstOrDefault(e => e.CveRecords?.Count > 0);

            // Get latest patch directory path for release.json link
            var latestPatchSummary = summary.PatchReleases.FirstOrDefault(p => p.PatchVersion == latestPatchVersion);

            // Build ordered links for major version index (lean navigation hub)
            var orderedMajorVersionLinks = new Dictionary<string, HalLink>();

            // 1. Add HAL+JSON links from base mappings first (Type is null for HAL+JSON)
            // Strip title from self link (href is sufficient)
            foreach (var link in majorVersionLinks.Where(kvp => kvp.Value.Type == null))
            {
                orderedMajorVersionLinks[link.Key] = link.Key == HalTerms.Self
                    ? new HalLink(link.Value.Href)
                    : link.Value;
            }

            // 2. Add SDK links for supported versions (8.0+) - these are HAL+JSON
            if (IsVersionSdkSupported(majorVersionDirName))
            {
                var sdkIndexPath = Path.Combine(majorVersionDir, FileNames.Directories.Sdk, FileNames.Index);
                var relativeSdkIndexPath = Path.GetRelativePath(outputDir, sdkIndexPath);
                var pathValue = "/" + relativeSdkIndexPath.Replace("\\", "/");
                orderedMajorVersionLinks[LinkRelations.LatestSdk] = new HalLink($"{Location.GitHubBaseUri}{relativeSdkIndexPath}")
                {
                    Title = $".NET SDK {majorVersionDirName} Release Information",
                };

                // 2a. Add downloads link to downloads directory
                var downloadsIndexPath = $"{majorVersionDirName}/{FileNames.Directories.Downloads}/{FileNames.Index}";
                orderedMajorVersionLinks["downloads"] = new HalLink($"{Location.GitHubBaseUri}{downloadsIndexPath}")
                {
                    Title = $".NET {majorVersionDirName} Downloads",
                };
            }

            // 2b. Add releases-index link (one level up to root index)
            orderedMajorVersionLinks[LinkRelations.ReleasesIndex] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Index}")
            {
                Title = LinkTitles.ReleaseIndex,
            };

            // 3. Add latest and latest-security HAL+JSON links
            if (latestPatch != null)
            {
                var latestPatchIndexPath = $"{majorVersionDirName}/{latestPatch.Version}/{FileNames.Index}";
                orderedMajorVersionLinks["latest"] = new HalLink($"{Location.GitHubBaseUri}{latestPatchIndexPath}")
                {
                    Title = LinkTitles.LatestPatch,
                };
            }

            if (latestSecurityPatch != null)
            {
                var latestSecurityPatchIndexPath = $"{majorVersionDirName}/{latestSecurityPatch.Version}/{FileNames.Index}";
                orderedMajorVersionLinks["latest-security"] = new HalLink($"{Location.GitHubBaseUri}{latestSecurityPatchIndexPath}")
                {
                    Title = LinkTitles.LatestSecurityPatch,
                };
            }

            // 4. Add latest-release-json link (small file, LLM-friendly)
            if (latestPatchSummary?.PatchDirPath != null)
            {
                var latestReleaseJsonPath = $"{latestPatchSummary.PatchDirPath}/{FileNames.Release}";
                orderedMajorVersionLinks["latest-release-json"] = new HalLink($"{Location.GitHubBaseUri}{latestReleaseJsonPath}")
                {
                    Title = $"Latest release information ({latestPatchVersion})",
                    Type = MediaType.Json
                };
            }

            // 5. Add compatibility-json link if the file exists (high-value for upgrade decisions)
            var compatibilityPath = Path.Combine(majorVersionDir, FileNames.Compatibility);
            if (File.Exists(compatibilityPath))
            {
                var compatibilityRelativePath = $"{majorVersionDirName}/{FileNames.Compatibility}";
                orderedMajorVersionLinks[LinkRelations.CompatibilityJson] = new HalLink($"{Location.GitHubBaseUri}{compatibilityRelativePath}")
                {
                    Title = $".NET {majorVersionDirName} Compatibility",
                    Type = MediaType.Json
                };
            }

            // 6. Add target-frameworks-json link if the file exists
            var targetFrameworksPath = Path.Combine(majorVersionDir, FileNames.TargetFrameworks);
            if (File.Exists(targetFrameworksPath))
            {
                var targetFrameworksRelativePath = $"{majorVersionDirName}/{FileNames.TargetFrameworks}";
                orderedMajorVersionLinks[LinkRelations.TargetFrameworksJson] = new HalLink($"{Location.GitHubBaseUri}{targetFrameworksRelativePath}")
                {
                    Title = $".NET {majorVersionDirName} Target Frameworks",
                    Type = MediaType.Json
                };
            }

            majorVersionLinks = orderedMajorVersionLinks;

            // write major version index.json if there are patch releases found
            var majorIndexPath = Path.Combine(outputMajorVersionDir, FileNames.Index);
            var relativeMajorIndexPath = Path.GetRelativePath(inputDir, Path.Combine(majorVersionDir, FileNames.Index));

            // Build years array for embedded data
            List<TimelineYear>? yearsEmbedded = null;
            if (releaseYears.Count > 0)
            {
                yearsEmbedded = releaseYears.Select(year =>
                {
                    var yearHistoryPath = $"{FileNames.Directories.Timeline}/{year}/{FileNames.Index}";
                    var yearLinks = new Dictionary<string, HalLink>
                    {
                        [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{yearHistoryPath}")
                    };
                    return new TimelineYear(year.ToString(), yearLinks);
                }).ToList();
            }
            
            var patchVersionIndex = new PatchReleaseVersionIndex(
                ReleaseKind.MajorVersionIndex,
                $".NET {summary.MajorVersionLabel.Replace(".NET ", string.Empty)} Release Index")
            {
                TargetFramework = generatedManifest.TargetFramework,
                Latest = latestPatch?.Version,
                LatestSecurity = latestSecurityPatch?.Version,
                ReleaseType = lifecycle?.ReleaseType,
                SupportPhase = lifecycle?.Phase,
                Supported = lifecycle?.Supported,
                GaDate = lifecycle?.GaDate,
                EolDate = lifecycle?.EolDate,
                Links = HalHelpers.OrderLinks(majorVersionLinks),
                Embedded = patchEntries.Count > 0 || yearsEmbedded != null ? new PatchReleaseVersionIndexEmbedded(
                    patchEntries.Select(e => {
                        var year = e.Lifecycle?.GaDate.Year.ToString("D4");
                        var month = e.Lifecycle?.GaDate.Month.ToString("D2");

                        // Build links - start with existing links
                        var links = new Dictionary<string, HalLink>(e.Links);

                        // Add release-month link if we have date info
                        if (year != null && month != null)
                        {
                            var monthIndexPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Index}";
                            links[LinkRelations.ReleaseMonth] = new HalLink($"{Location.GitHubBaseUri}{monthIndexPath}")
                            {
                                Title = LinkTitles.TimelineMonthIndex,
                            };

                            // Add cve-json link for security patches
                            if (e.CveRecords?.Count > 0)
                            {
                                var cveJsonPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Cve}";
                                links[LinkRelations.CveJson] = new HalLink($"{Location.GitHubBaseUri}{cveJsonPath}")
                                {
                                    Title = LinkTitles.CveRecordsJson,
                                    Type = MediaType.Json
                                };
                            }
                        }

                        return new PatchReleaseVersionIndexEntry(
                            e.Version,
                            e.Lifecycle?.GaDate,
                            year,
                            month,
                            e.CveRecords?.Count > 0,
                            e.CveRecords?.Count ?? 0,
                            e.CveRecords,
                            e.Lifecycle?.Phase,
                            e.SdkVersions?.FirstOrDefault(),
                            HalHelpers.OrderLinks(links));
                    }).ToList())
                {
                    Years = yearsEmbedded
                } : null
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
            await File.WriteAllTextAsync(patchIndexPath, finalPatchIndexJson);

            // Same links as the major version index, but with a different base directory (to force different pathing)
            // NOTE: Do NOT add latest-patch or latest-month links here - those change monthly
            // and would cause the root index.json to change frequently. Those links belong
            // in the major version indexes (e.g., 8.0/index.json) instead.
            var majorVersionWithinAllReleasesIndexLinks = halLinkGenerator.Generate(
                majorVersionDir,
                MainFileMappings.Values,
                (fileLink, key) => key == HalTerms.Self ? summary.MajorVersionLabel : fileLink.Title);

            // Major version entries use minimal lifecycle properties for root index stability.
            // Omitted properties (available in major version indexes like 8.0/index.json):
            // - Phase: changes frequently (preview->go-live->active->maintenance)
            // - GaDate/EolDate: static, fetch from referenced resource
            // - Years: changes every January for active releases
            // - Path/Title in self links: redundant with href, keeps entries lean
            // Root index focuses on: release_type, supported (for quick filtering)

            // Strip title and type from self links for root index entries (href is sufficient)
            var minimalLinks = majorVersionWithinAllReleasesIndexLinks.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Key == HalTerms.Self
                    ? new HalLink(kvp.Value.Href)  // Self link: href only
                    : new HalLink(kvp.Value.Href) { Title = kvp.Value.Title, Type = kvp.Value.Type });

            var majorEntry = new MajorReleaseVersionIndexEntry(majorVersionDirName)
            {
                ReleaseType = lifecycle?.ReleaseType,
                Supported = lifecycle?.Supported,
                Links = HalHelpers.OrderLinks(minimalLinks)
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

            // Add HAL+JSON links first (Type is null for HAL+JSON)
            foreach (var link in rootLinks.Where(kvp => kvp.Value.Type == null))
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
                    Title = LinkTitles.MajorVersionIndex,
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
                    Title = LinkTitles.LatestLts,
                };
            }

            // NOTE: Do NOT add latest-year link here - it changes every January
            // and would cause the root index.json to change annually. The timeline-index
            // link provides access to the timeline, which has its own latest-year link.

            // Add non-HAL+JSON links (markdown files) after
            foreach (var link in rootLinks.Where(kvp => kvp.Value.Type != MediaType.HalJson))
            {
                orderedRootLinks[link.Key] = link.Value;
            }

            // Add llms-reference link last - describes the graph for LLM consumption
            // Note: llms/ is at repo root, not inside release-notes/
            var repoBaseUri = Location.GitHubBaseUri.Replace("/release-notes/", "/");
            orderedRootLinks["llms-reference"] = new HalLink($"{repoBaseUri}llms/reference.md")
            {
                Title = "LLM Quick Reference",
                Type = MediaType.Markdown
            };

            rootLinks = orderedRootLinks;
        }

        Console.WriteLine($"Found {rootLinks.Count} root links in {inputDir}");

        // Create the major releases index; release-notes/index.json
        var rootIndexPath = Path.Combine(outputDir, FileNames.Index);

        // NOTE: Do NOT include LatestYear property - it changes every January
        // and would cause the root index.json to change annually. The timeline-index
        // link provides access to timeline/index.json which has its own latest_year.
        var majorIndex = new MajorReleaseVersionIndex(
                ReleaseKind.ReleasesIndex,
                IndexTitles.VersionIndexTitle)
        {
            Latest = latestRelease?.Version,
            LatestLts = latestLtsRelease?.Version,
            Links = HalHelpers.OrderLinks(rootLinks),
            Embedded = new MajorReleaseVersionIndexEmbedded([.. majorEntries.OrderByDescending(e => e.Version, numericStringComparer)])
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
        await File.WriteAllTextAsync(rootMajorIndexPath, finalMajorIndexJson);
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
        var inputRoot = Path.GetDirectoryName(rootDir) ?? rootDir;

        // Convert to list for index-based access (for prev/next navigation)
        var summaryList = summaries.ToList();

        for (int i = 0; i < summaryList.Count; i++)
        {
            var summary = summaryList[i];
            if (!summaryTable.ContainsKey(summary.PatchVersion))
            {
                continue;
            }

            // Use PatchDirPath if available (handles preview/rc paths), otherwise fall back to PatchVersion
            if (summary.PatchDirPath == null)
            {
                continue;
            }

            // PatchDirPath is relative to the input root (e.g., "10.0/10.0.0" or "10.0/preview/preview1")
            // We need to construct the full path using the major version directory's parent
            var patchDir = Path.Combine(inputRoot, summary.PatchDirPath);

            var releaseJson = Path.Combine(patchDir, FileNames.Release);
            if (!File.Exists(releaseJson))
            {
                continue;
            }
            var relativePath = Path.GetRelativePath(inputRoot, releaseJson);
            var urlRelativePath = Path.GetRelativePath(urlRootDir ?? inputRoot, releaseJson);
            var releaseJsonPathValue = "/" + relativePath.Replace("\\", "/");

            // Create links - self now points to index.json (href only), with separate link to release.json
            // Use PatchDirPath for the URL path (handles preview/rc structure)
            var patchIndexPath = $"{summary.PatchDirPath}/{FileNames.Index}";
            var links = new Dictionary<string, HalLink>
                {
                    { HalTerms.Self, new HalLink(IndexHelpers.GetProdPath(patchIndexPath)) }
                };

            // Determine CVE IDs - prefer cve.json (authoritative), fall back to releases.json
            IReadOnlyList<string>? cveIds = null;

            // First, try to get CVE IDs from cve.json (authoritative source)
            var releaseDate = summary.ReleaseDate;
            var releaseDateOffset = new DateTimeOffset(releaseDate.Year, releaseDate.Month, releaseDate.Day, 0, 0, 0, TimeSpan.Zero);
            var cveRecords = await CveHandler.CveLoader.LoadCveRecordsForReleaseDateAsync(inputRoot, releaseDateOffset);

            if (cveRecords?.ReleaseCves != null && cveRecords.ReleaseCves.TryGetValue(majorVersion, out var cveIdsFromCveJson))
            {
                // Use cve.json as authoritative source - it correctly excludes package-only CVEs
                cveIds = cveIdsFromCveJson.ToList();

                // Log if there's a mismatch with releases.json (for awareness)
                var cveIdsFromRelease = summary.CveList?.Select(cve => cve.CveId).ToList();
                if (cveIdsFromRelease?.Count > 0)
                {
                    CveHandler.CveTransformer.ValidateCveData(
                        summary.PatchVersion,
                        cveIdsFromRelease,
                        cveIds as IReadOnlyList<string>,
                        $"timeline/{releaseDate.Year:D4}/{releaseDate.Month:D2}/cve.json");
                }
            }
            else if (summary.CveList?.Count > 0)
            {
                // Fall back to releases.json if cve.json not available
                cveIds = summary.CveList.Select(cve => cve.CveId).ToList();
            }

            // Create simplified lifecycle for patch releases (per spec: only phase and release-date)
            SupportPhase patchPhase;
            DateTimeOffset patchReleaseDate;

            // Use actual patch release date from summary
            var releaseDateOnly = summary.ReleaseDate;
            patchReleaseDate = new DateTimeOffset(releaseDateOnly.Year, releaseDateOnly.Month, releaseDateOnly.Day, 0, 0, 0, TimeSpan.Zero);

            // Determine phase based on version string first (previews and RCs have specific phases)
            if (summary.PatchVersion.Contains("-preview."))
            {
                patchPhase = SupportPhase.Preview;
            }
            else if (summary.PatchVersion.Contains("-rc."))
            {
                patchPhase = SupportPhase.GoLive;
            }
            else if (majorVersionLifecycle != null)
            {
                // GA releases inherit phase from major version lifecycle
                patchPhase = majorVersionLifecycle.Phase;
            }
            else
            {
                // Fallback: determine phase based on whether the release date is in the future
                patchPhase = patchReleaseDate > DateTimeOffset.UtcNow ? SupportPhase.Preview : SupportPhase.Active;
            }

            var patchLifecycle = new PatchLifecycle(patchPhase, patchReleaseDate);

            // Determine prev patch (within same major version)
            // summaryList is ordered newest to oldest, so prev is i+1 (older)
            // NOTE: No "next" - patch indexes are immutable; navigate via "latest" and walk backwards
            var prevSummary = i + 1 < summaryList.Count ? summaryList[i + 1] : null;

            // Find prev-security patch (previous patch with CVEs)
            // Search from i+1 onwards to find the first patch with security fixes
            PatchReleaseSummary? prevSecuritySummary = null;
            for (int j = i + 1; j < summaryList.Count; j++)
            {
                var candidate = summaryList[j];
                // Check if this patch had CVEs (via CveList from releases.json)
                if (candidate.CveList?.Count > 0)
                {
                    prevSecuritySummary = candidate;
                    break;
                }
            }

            // Always generate patch detail index (for all patches, not just those with CVEs)
            await GeneratePatchDetailIndexAsync(
                patchDir,
                outputDir,
                urlRootDir ?? inputRoot,
                majorVersion,
                summary.PatchVersion,
                summary.PatchDirPath,
                patchLifecycle,
                cveIds,
                prevSummary?.PatchVersion,
                prevSummary?.PatchDirPath,
                prevSecuritySummary?.PatchVersion,
                prevSecuritySummary?.PatchDirPath);

            // Get SDK versions from components
            var sdkVersions = summary.Components?
                .Where(c => c.Name == "SDK")
                .Select(c => c.Version)
                .OrderByDescending(v => v, StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering))
                .ToList();
            if (sdkVersions?.Count == 0) sdkVersions = null;

            var indexEntry = new ReleaseVersionIndexEntry(summary.PatchVersion, links)
            {
                CveRecords = cveIds,
                Lifecycle = patchLifecycle,
                SdkVersions = sdkVersions
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
        string patchDirPath,  // The relative path like "10.0/10.0.0" or "10.0/preview/preview1"
        PatchLifecycle lifecycle,
        IReadOnlyList<string>? cveIds,
        string? prevPatchVersion,
        string? prevPatchDirPath,
        string? prevSecurityPatchVersion,
        string? prevSecurityPatchDirPath)
    {
        // Create patch detail index links - HAL+JSON links first, then JSON
        var links = new Dictionary<string, HalLink>
        {
            // HAL+JSON links first - self link has no title (inferable)
            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/{FileNames.Index}"),
            [LinkRelations.ReleaseMajor] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{FileNames.Index}")
            {
                Title = LinkTitles.MajorVersionIndex,
            }
        };

        // Add releases-index link (grandparent - root index)
        links[LinkRelations.ReleasesIndex] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Index}")
        {
            Title = LinkTitles.ReleaseIndex,
        };

        // Add latest-sdk link if version supports SDK (8.0+)
        if (IsVersionSdkSupported(majorVersion))
        {
            links[LinkRelations.LatestSdk] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{FileNames.Directories.Sdk}/{FileNames.Index}")
            {
                Title = LinkTitles.SdkIndex,
            };
        }

        // Add prev/next links for patch navigation within the same major version
        if (prevPatchVersion != null && prevPatchDirPath != null)
        {
            links[HalTerms.Prev] = new HalLink($"{Location.GitHubBaseUri}{prevPatchDirPath}/{FileNames.Index}")
            {
                Title = LinkTitles.PatchIndex,
            };
        }

        // Add prev-security link for security patch navigation
        if (prevSecurityPatchVersion != null && prevSecurityPatchDirPath != null)
        {
            links[LinkRelations.PrevSecurity] = new HalLink($"{Location.GitHubBaseUri}{prevSecurityPatchDirPath}/{FileNames.Index}")
            {
                Title = LinkTitles.LatestSecurityPatch,
            };
        }

        // NOTE: No "next" links - patch indexes are immutable once created.
        // Navigation pattern: start from "latest" on major version index and walk backwards via "prev" links.
        // For security patches, start from "latest-security" and walk backwards via "prev-security" links.

        // release-month will be added below after we determine the release date (HAL+JSON)
        // Then JSON links (release-json, cve-json) will be added at the end

        // Add release notes markdown links to root _links
        var versionMdPath = Path.Combine(patchDir, $"{patchVersion}.md");
        var readmePath = Path.Combine(patchDir, "README.md");

        if (File.Exists(versionMdPath))
        {
            var mdFileName = $"{patchVersion}.md";
            links["release-notes-markdown"] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/{mdFileName}")
            {
                Title = LinkTitles.ReleaseNotes,
                Type = MediaType.Markdown
            };
            links["release-notes-markdown-rendered"] = new HalLink($"https://github.com/dotnet/core/blob/main/release-notes/{patchDirPath}/{mdFileName}")
            {
                Title = $"{LinkTitles.ReleaseNotes} (Rendered)",
                Type = MediaType.Markdown
            };
        }
        else if (File.Exists(readmePath))
        {
            links["release-notes-markdown"] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/README.md")
            {
                Title = LinkTitles.ReleaseNotes,
                Type = MediaType.Markdown
            };
            links["release-notes-markdown-rendered"] = new HalLink($"https://github.com/dotnet/core/blob/main/release-notes/{patchDirPath}/README.md")
            {
                Title = $"{LinkTitles.ReleaseNotes} (Rendered)",
                Type = MediaType.Markdown
            };
        }

        // Load SDK versions from release.json and build SDK feature band entries
        List<string>? sdkVersionsList = null;
        List<SdkFeatureBandEntry>? sdkFeatureBandEntries = null;
        string? highestSdkVersion = null;
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
                    // Sort SDK versions descending to get highest first
                    var numericComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
                    sdkVersions = sdkVersions.OrderByDescending(v => v, numericComparer).ToList();

                    sdkVersionsList = sdkVersions;
                    highestSdkVersion = sdkVersions.First();
                    sdkFeatureBandEntries = [];

                    // Build SDK feature band entries (same shape as sdk/index.json)
                    foreach (var sdkVersion in sdkVersions)
                    {
                        var parts = sdkVersion.Split('.');
                        if (parts.Length < 3) continue;

                        var featureBand = $"{parts[0]}.{parts[1]}.{parts[2][0]}xx";

                        // Build links for this feature band entry
                        var bandLinks = new Dictionary<string, HalLink>
                        {
                            ["downloads"] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{FileNames.Directories.Downloads}/sdk-{featureBand}.json")
                            {
                                Title = $".NET SDK {featureBand} Downloads",
                                Type = MediaType.Json
                            }
                        };

                        // Add release-month link if we have lifecycle date
                        if (lifecycle?.GaDate != null)
                        {
                            var year = lifecycle.GaDate.Year.ToString("D4");
                            var month = lifecycle.GaDate.Month.ToString("D2");
                            var monthIndexPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Index}";
                            bandLinks["release-month"] = new HalLink($"{Location.GitHubBaseUri}{monthIndexPath}")
                            {
                                Title = LinkTitles.TimelineMonthIndex,
                            };
                        }

                        // Add release-patch link (to this patch release)
                        bandLinks["release-patch"] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/{FileNames.Index}")
                        {
                            Title = patchVersion,
                        };

                        sdkFeatureBandEntries.Add(new SdkFeatureBandEntry(
                            sdkVersion,                  // version (latest SDK in band for this patch)
                            featureBand,                 // band (e.g., "9.0.3xx")
                            lifecycle?.GaDate,           // date
                            $".NET SDK {featureBand}",   // label
                            lifecycle?.Phase,            // support_phase
                            HalHelpers.OrderLinks(bandLinks)));
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
        bool hasCveDisclosures = false;

        if (lifecycle?.GaDate != null)
        {
            var releaseDate = lifecycle.GaDate;
            var year = releaseDate.Year.ToString("D4");
            var month = releaseDate.Month.ToString("D2");
            timelineCveJsonPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Cve}";
            timelineMonthIndexPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Index}";

            // Add link to release month (HAL+JSON - added before JSON links)
            links[LinkRelations.ReleaseMonth] = new HalLink($"{Location.GitHubBaseUri}{timelineMonthIndexPath}")
            {
                Title = LinkTitles.TimelineMonthIndex,
            };

            // Load CVE records from timeline directory
            var cveRecords = await CveHandler.CveLoader.LoadCveRecordsForReleaseDateAsync(inputDir, releaseDate);

            if (cveRecords != null)
            {
                // Filter by major version (e.g., "9.0")
                var filteredCveRecords = CveHandler.CveTransformer.FilterByRelease(cveRecords, majorVersion);

                if (filteredCveRecords?.Disclosures.Count > 0)
                {
                    hasCveDisclosures = true;

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
                            filteredCveRecords.SeverityCves,
                            filteredCveRecords.CveReleases,
                            filteredCveRecords.CveCommits
                        )
                    );
                }
            }
        }

        // Now add JSON links (after all HAL+JSON links)
        links["release-json"] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/{FileNames.Release}")
        {
            Title = $"{patchVersion} Release Information",
            Type = MediaType.Json
        };

        // Add CVE JSON link if there are disclosures
        if (hasCveDisclosures && timelineCveJsonPath != null)
        {
            links[LinkRelations.CveJson] = new HalLink($"{Location.GitHubBaseUri}{timelineCveJsonPath}")
            {
                Title = LinkTitles.CveRecordsJson,
                Type = MediaType.Json
            };

            // Add CVE markdown links (raw and rendered)
            // timelineCveJsonPath is like "timeline/2025/01/cve.json", change to "timeline/2025/01/cve.md"
            var timelineCveMdPath = timelineCveJsonPath.Replace("cve.json", "cve.md");
            links["cve-markdown"] = new HalLink($"{Location.GitHubBaseUri}{timelineCveMdPath}")
            {
                Title = LinkTitles.CveRecordsJson,
                Type = MediaType.Markdown
            };
            links["cve-markdown-rendered"] = new HalLink($"https://github.com/dotnet/core/blob/main/release-notes/{timelineCveMdPath}")
            {
                Title = $"{LinkTitles.CveRecordsJson} (Rendered)",
                Type = MediaType.Markdown
            };
        }

        // Build embedded content
        // Extract sorted CVE IDs from disclosures (source of truth from cve.json)
        IReadOnlyList<string>? sortedCveIds = null;
        if (cveDisclosures != null && cveDisclosures.Count > 0)
        {
            sortedCveIds = cveDisclosures.Select(d => d.Id).ToList();
        }

        PatchDetailIndexEmbedded? embedded = null;
        if (sdkFeatureBandEntries != null || cveDisclosures != null)
        {
            embedded = new PatchDetailIndexEmbedded
            {
                SdkRelease = sdkFeatureBandEntries?.FirstOrDefault(),  // highest SDK (list is sorted descending)
                SdkFeatureBands = sdkFeatureBandEntries,
                Disclosures = cveDisclosures
            };
        }

        var patchDetailIndex = new PatchDetailIndex(
            ReleaseKind.PatchVersionIndex,
            $".NET {patchVersion} Patch Index",
            patchVersion,
            lifecycle?.GaDate,
            lifecycle?.Phase,
            cveIds?.Count > 0,
            cveIds?.Count ?? 0,
            sortedCveIds)
        {
            SdkRelease = highestSdkVersion,
            SdkFeatureBands = sdkVersionsList,
            Links = HalHelpers.OrderLinks(links),
            Embedded = embedded
        };

        // Serialize
        var patchDetailJson = JsonSerializer.Serialize(
            patchDetailIndex,
            ReleaseVersionIndexSerializerContext.Default.PatchDetailIndex);

        // Add schema reference
        var schemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.PatchDetailIndex}";
        var updatedJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(patchDetailJson, schemaUri);

        // Write to file - use patchDirPath for correct output location (handles preview/rc paths)
        var outputPatchDir = Path.Combine(outputDir, patchDirPath);
        if (!Directory.Exists(outputPatchDir))
        {
            Directory.CreateDirectory(outputPatchDir);
        }

        var indexPath = Path.Combine(outputPatchDir, FileNames.Index);
        var finalJson = (updatedJson ?? patchDetailJson) + '\n';
        await File.WriteAllTextAsync(indexPath, finalJson);
    }
}
