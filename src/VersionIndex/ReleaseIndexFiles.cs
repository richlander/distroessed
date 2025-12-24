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
        {FileNames.Compatibility, new FileLink(FileNames.Compatibility, LinkTitles.Compatibility, LinkStyle.Prod) },
        {FileNames.TargetFrameworks, new FileLink(FileNames.TargetFrameworks, LinkTitles.TargetFrameworks, LinkStyle.Prod) },
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
                (fileLink, key) => key switch
                {
                    HalTerms.Self => summary.MajorVersionLabel,
                    LinkRelations.Manifest => $"Manifest - .NET {majorVersionDirName}",
                    _ => fileLink.Title
                });

            // Generate patch version index; release-notes/8.0/index.json
            var patchEntries = await GetPatchIndexEntriesAsync(summaryTable[majorVersionDirName].PatchReleases, new PathContext(majorVersionDir, inputDir), lifecycle, outputDir, majorVersionDirName);

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
                // Add downloads link to downloads directory
                var downloadsIndexPath = $"{majorVersionDirName}/{FileNames.Directories.Downloads}/{FileNames.Index}";
                orderedMajorVersionLinks[LinkRelations.Downloads] = new HalLink($"{Location.GitHubBaseUri}{downloadsIndexPath}")
                {
                    Title = $"Downloads - .NET {majorVersionDirName}",
                };
            }

            // 2b. Add releases-index link (one level up to root index)
            orderedMajorVersionLinks[LinkRelations.Root] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Index}")
            {
                Title = LinkTitles.DotNetReleaseIndex,
            };

            // 3. Add latest and latest-security HAL+JSON links
            if (latestPatch != null)
            {
                var latestPatchIndexPath = $"{majorVersionDirName}/{latestPatch.Version}/{FileNames.Index}";
                orderedMajorVersionLinks[LinkRelations.LatestPatch] = new HalLink($"{Location.GitHubBaseUri}{latestPatchIndexPath}")
                {
                    Title = $"Latest patch - {latestPatch.Version}",
                };

                // Add latest-month link to timeline month for the latest patch
                if (latestPatch.Lifecycle?.GaDate != null)
                {
                    var patchDate = latestPatch.Lifecycle.GaDate;
                    var patchYear = patchDate.Year.ToString("D4");
                    var patchMonth = patchDate.Month.ToString("D2");
                    orderedMajorVersionLinks[LinkRelations.LatestMonth] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{patchYear}/{patchMonth}/{FileNames.Index}")
                    {
                        Title = $"Latest month - {IndexTitles.FormatMonthYear(patchYear, patchMonth)}",
                    };
                }
            }

            if (latestSecurityPatch != null)
            {
                var latestSecurityPatchIndexPath = $"{majorVersionDirName}/{latestSecurityPatch.Version}/{FileNames.Index}";
                orderedMajorVersionLinks[LinkRelations.LatestSecurityPatch] = new HalLink($"{Location.GitHubBaseUri}{latestSecurityPatchIndexPath}")
                {
                    Title = $"Latest security patch - {latestSecurityPatch.Version}",
                };

                // Add latest-security-month and latest-cve-json links
                if (latestSecurityPatch.Lifecycle?.GaDate != null)
                {
                    var securityPatchDate = latestSecurityPatch.Lifecycle.GaDate;
                    var securityYear = securityPatchDate.Year.ToString("D4");
                    var securityMonth = securityPatchDate.Month.ToString("D2");

                    orderedMajorVersionLinks[LinkRelations.LatestSecurityMonth] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{securityYear}/{securityMonth}/{FileNames.Index}")
                    {
                        Title = $"Latest security month - {IndexTitles.FormatMonthYear(securityYear, securityMonth)}",
                    };

                    // Add latest-cve-json link for direct access to CVE data (only if cve.json exists)
                    var cveJsonPath = Path.Combine(inputDir, FileNames.Directories.Timeline, securityYear, securityMonth, FileNames.Cve);
                    if (File.Exists(cveJsonPath))
                    {
                        orderedMajorVersionLinks[LinkRelations.LatestCveJson] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Directories.Timeline}/{securityYear}/{securityMonth}/{FileNames.Cve}")
                        {
                            Title = $"Latest CVE records - {IndexTitles.FormatMonthYear(securityYear, securityMonth)}",
                            Type = MediaType.Json
                        };
                    }
                }
            }

            majorVersionLinks = orderedMajorVersionLinks;

            // Generate SDK feature band entries
            List<SdkFeatureBandEntry>? sdkFeatureBandEntries = null;
            if (summary.SdkBands?.Count > 0)
            {
                sdkFeatureBandEntries = summary.SdkBands
                    .OrderByDescending(b => b.Version, numericStringComparer)
                    .Select(sdkBand =>
                    {
                        var bandVersion = sdkBand.Version[..5] + "xx"; // e.g., "8.0.4xx"

                        // Find the latest SDK version in this feature band
                        var latestInBand = summary.PatchReleases
                            .SelectMany(p => p.Components
                                .Where(c => c.Name.Equals("sdk", StringComparison.OrdinalIgnoreCase) &&
                                           c.Version.StartsWith(sdkBand.Version[..5]))
                                .Select(c => (Component: c, PatchRelease: p)))
                            .OrderByDescending(x => x.Component.Version, numericStringComparer)
                            .FirstOrDefault();

                        var latestSdkInBand = latestInBand.Component?.Version;
                        var latestPatchRelease = latestInBand.PatchRelease;

                        // Build links for this feature band
                        // Note: No titles or types in _embedded links - context established by parent
                        var bandLinks = new Dictionary<string, HalLink>
                        {
                            ["downloads"] = new HalLink($"{Location.GitHubBaseUri}{majorVersionDirName}/{FileNames.Directories.Downloads}/sdk-{bandVersion}.json")
                        };

                        // Add patch link (to the patch release this SDK is part of)
                        if (latestPatchRelease?.PatchDirPath != null)
                        {
                            var patchIndexPath = $"{latestPatchRelease.PatchDirPath}/{FileNames.Index}";
                            bandLinks[LinkRelations.Patch] = new HalLink($"{Location.GitHubBaseUri}{patchIndexPath}");
                        }

                        // Add month link
                        if (latestPatchRelease != null)
                        {
                            var year = latestPatchRelease.ReleaseDate.Year.ToString("D4");
                            var month = latestPatchRelease.ReleaseDate.Month.ToString("D2");
                            var monthIndexPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Index}";
                            bandLinks[LinkRelations.Month] = new HalLink($"{Location.GitHubBaseUri}{monthIndexPath}");
                        }

                        return new SdkFeatureBandEntry(
                            latestSdkInBand ?? bandVersion,  // version (latest SDK in band)
                            bandVersion,                      // band (e.g., "9.0.3xx")
                            latestPatchRelease != null
                                ? new DateTimeOffset(latestPatchRelease.ReleaseDate.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-8))
                                : null,
                            $".NET SDK {bandVersion}",
                            sdkBand.SupportPhase,
                            HalHelpers.OrderLinks(bandLinks));
                    })
                    .ToList();
            }

            // write major version index.json if there are patch releases found
            var majorIndexPath = Path.Combine(outputMajorVersionDir, FileNames.Index);
            var relativeMajorIndexPath = Path.GetRelativePath(inputDir, Path.Combine(majorVersionDir, FileNames.Index));

            var patchVersionIndex = new PatchReleaseVersionIndex(
                ReleaseKind.Major,
                $".NET Major Release Index - {summary.MajorVersionLabel.Replace(".NET ", string.Empty)}")
            {
                TargetFramework = generatedManifest.TargetFramework,
                LatestPatch = latestPatch?.Version,
                LatestPatchDate = latestPatch?.Lifecycle?.GaDate,
                LatestSecurityPatch = latestSecurityPatch?.Version,
                LatestSecurityPatchDate = latestSecurityPatch?.Lifecycle?.GaDate,
                ReleaseType = lifecycle?.ReleaseType,
                SupportPhase = lifecycle?.Phase,
                Supported = lifecycle?.Supported,
                GaDate = lifecycle?.GaDate,
                EolDate = lifecycle?.EolDate,
                Links = HalHelpers.OrderLinks(majorVersionLinks),
                Embedded = patchEntries.Count > 0 ? new PatchReleaseVersionIndexEmbedded(
                    patchEntries
                    .Where(e => e.Lifecycle?.GaDate != null && e.Lifecycle?.Phase != null)
                    .Select(e => {
                        var gaDate = e.Lifecycle!.GaDate;
                        var phase = e.Lifecycle!.Phase;
                        var year = gaDate.Year.ToString("D4");
                        var month = gaDate.Month.ToString("D2");

                        // Build links - start with existing links
                        var links = new Dictionary<string, HalLink>(e.Links);

                        // Add month link for timeline navigation
                        // Note: No titles in _embedded links - context established by parent
                        var monthIndexPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Index}";
                        links[LinkRelations.Month] = new HalLink($"{Location.GitHubBaseUri}{monthIndexPath}");

                        // Add cve-json and security-disclosures links for security patches
                        if (e.CveRecords?.Count > 0)
                        {
                            var cveJsonPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Cve}";
                            links[LinkRelations.CveJson] = new HalLink($"{Location.GitHubBaseUri}{cveJsonPath}")
                            {
                                Type = MediaType.Json
                            };

                            // Add security-disclosures as semantic alias to month index (competes better with CVE/security queries)
                            links[LinkRelations.SecurityDisclosures] = new HalLink($"{Location.GitHubBaseUri}{monthIndexPath}");
                        }

                        // Note: CVE IDs (cve_records) are intentionally omitted from patch entries.
                        // CVEs are a timeline concept - use month or cve-json link.
                        return new PatchReleaseVersionIndexEntry(
                            e.Version,
                            gaDate,
                            year,
                            month,
                            e.CveRecords?.Count > 0,
                            phase,
                            HalHelpers.OrderLinks(links))
                        {
                            SdkVersion = e.SdkVersions?.FirstOrDefault()
                        };
                    }).ToList())
                {
                    SdkFeatureBands = sdkFeatureBandEntries
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
                (fileLink, key) => key switch
                {
                    HalTerms.Self => summary.MajorVersionLabel,
                    LinkRelations.Manifest => $"Manifest - .NET {majorVersionDirName}",
                    _ => fileLink.Title
                });

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
                orderedRootLinks[LinkRelations.LatestMajor] = new HalLink($"{Location.GitHubBaseUri}{latestRelease.Version}/{FileNames.Index}")
                {
                    Title = $"Latest major release - .NET {latestRelease.Version}",
                };
            }

            // Find latest stable LTS release (uses lifecycle.ReleaseType, not version number heuristics)
            latestLtsRelease = latestLtsVersion != null
                ? majorEntries.FirstOrDefault(e => e.Version == latestLtsVersion)
                : null;

            if (latestLtsRelease != null)
            {
                orderedRootLinks[LinkRelations.LatestLtsMajor] = new HalLink($"{Location.GitHubBaseUri}{latestLtsRelease.Version}/{FileNames.Index}")
                {
                    Title = $"Latest LTS major release - .NET {latestLtsRelease.Version}",
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

            rootLinks = orderedRootLinks;
        }

        Console.WriteLine($"Found {rootLinks.Count} root links in {inputDir}");

        // Create the major releases index; release-notes/index.json
        var rootIndexPath = Path.Combine(outputDir, FileNames.Index);

        // NOTE: Do NOT include LatestYear property - it changes every January
        // and would cause the root index.json to change annually. The timeline-index
        // link provides access to timeline/index.json which has its own latest_year.
        var majorIndex = new MajorReleaseVersionIndex(
                ReleaseKind.Root,
                IndexTitles.VersionIndexTitle)
        {
            LatestMajor = latestRelease?.Version,
            LatestLtsMajor = latestLtsRelease?.Version,
            Links = HalHelpers.OrderLinks(rootLinks),
            Embedded = new MajorReleaseVersionIndexEmbedded
            {
                Releases = [.. majorEntries.OrderByDescending(e => e.Version, numericStringComparer)]
            }
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

        // Determine if this is a GA release (for filtering previews from index)
        // RC releases are kept (they have go-live support), only previews are excluded from index
        var isGaRelease = majorVersionLifecycle?.Phase is SupportPhase.Active or SupportPhase.Maintenance or SupportPhase.Eol;

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

            // Skip patches without a directory (required for index generation)
            if (summary.PatchDirPath == null)
            {
                continue;
            }

            // PatchDirPath is relative to the input root (e.g., "10.0/10.0.0" or "10.0/preview/preview1")
            // We need to construct the full path using the major version directory's parent
            var patchDir = Path.Combine(inputRoot, summary.PatchDirPath);

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

            // Determine phase based on version string and release date
            // This synthesizes the phase AT TIME OF RELEASE (patch indexes are immutable)
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
                // GA patches: determine if Active or Maintenance based on release date
                // Maintenance phase starts 6 months before EOL
                var maintenanceStart = majorVersionLifecycle.EolDate.AddMonths(-6);
                if (patchReleaseDate >= maintenanceStart)
                {
                    patchPhase = SupportPhase.Maintenance;
                }
                else
                {
                    patchPhase = SupportPhase.Active;
                }
            }
            else
            {
                // Fallback: GA patches without lifecycle data are Active
                patchPhase = SupportPhase.Active;
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
            // Convert DateOnly to DateTimeOffset for prev dates
            DateTimeOffset? prevPatchDate = prevSummary != null
                ? new DateTimeOffset(prevSummary.ReleaseDate, TimeOnly.MinValue, TimeSpan.Zero)
                : null;
            DateTimeOffset? prevSecurityPatchDate = prevSecuritySummary != null
                ? new DateTimeOffset(prevSecuritySummary.ReleaseDate, TimeOnly.MinValue, TimeSpan.Zero)
                : null;

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
                prevPatchDate,
                prevSecuritySummary?.PatchVersion,
                prevSecuritySummary?.PatchDirPath,
                prevSecurityPatchDate);

            // Get SDK versions from components
            var sdkVersions = summary.Components?
                .Where(c => c.Name == "SDK")
                .Select(c => c.Version)
                .OrderByDescending(v => v, StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering))
                .ToList();
            if (sdkVersions?.Count == 0) sdkVersions = null;

            // Only add to index if not a preview release (for GA versions)
            // Previews are still generated (manifest/index files) but excluded from major version index
            var isPreview = summary.PatchVersion.Contains("-preview.");
            if (!isGaRelease || !isPreview)
            {
                var indexEntry = new ReleaseVersionIndexEntry(summary.PatchVersion, links)
                {
                    CveRecords = cveIds,
                    Lifecycle = patchLifecycle,
                    SdkVersions = sdkVersions
                };
                indexEntries.Add(indexEntry);
            }
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
        DateTimeOffset? prevPatchDate,
        string? prevSecurityPatchVersion,
        string? prevSecurityPatchDirPath,
        DateTimeOffset? prevSecurityPatchDate)
    {
        // Create patch detail index links - HAL+JSON links first, then JSON
        var links = new Dictionary<string, HalLink>
        {
            // HAL+JSON links first - self link has no title (inferable)
            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/{FileNames.Index}"),
            [LinkRelations.Major] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{FileNames.Index}")
            {
                Title = $".NET Major Release Index - {majorVersion}",
            }
        };

        // Add releases-index link (grandparent - root index)
        links[LinkRelations.Root] = new HalLink($"{Location.GitHubBaseUri}{FileNames.Index}")
        {
            Title = LinkTitles.DotNetReleaseIndex,
        };

        // Add downloads link if version supports SDK (8.0+)
        if (IsVersionSdkSupported(majorVersion))
        {
            links[LinkRelations.Downloads] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{FileNames.Directories.Downloads}/{FileNames.Index}")
            {
                Title = $"Downloads - .NET {majorVersion}",
            };
        }

        // Add prev links for patch navigation within the same major version
        if (prevPatchVersion != null && prevPatchDirPath != null)
        {
            links[LinkRelations.PrevPatch] = new HalLink($"{Location.GitHubBaseUri}{prevPatchDirPath}/{FileNames.Index}")
            {
                Title = $"Previous patch release - {prevPatchVersion}",
            };
        }

        // Add prev-security-patch link for security patch navigation
        if (prevSecurityPatchVersion != null && prevSecurityPatchDirPath != null)
        {
            links[LinkRelations.PrevSecurityPatch] = new HalLink($"{Location.GitHubBaseUri}{prevSecurityPatchDirPath}/{FileNames.Index}")
            {
                Title = $"Previous security patch release - {prevSecurityPatchVersion}",
            };
        }

        // NOTE: No "next" links - patch indexes are immutable once created.
        // Navigation pattern: start from "latest-patch" on major version index and walk backwards via "prev-patch" links.
        // For security patches, start from "latest-security-patch" and walk backwards via "prev-security-patch" links.

        // release-month will be added below after we determine the release date (HAL+JSON)
        // cve-json link will be added to index; release-json goes to manifest

        // Build manifest links (markdown/documentation links go in manifest.json)
        var manifestLinks = new Dictionary<string, HalLink>
        {
            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/{FileNames.Manifest}")
        };

        // Add release notes markdown links to manifest
        var versionMdPath = Path.Combine(patchDir, $"{patchVersion}.md");
        var readmePath = Path.Combine(patchDir, "README.md");

        if (File.Exists(versionMdPath))
        {
            var mdFileName = $"{patchVersion}.md";
            manifestLinks["release-notes-markdown"] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/{mdFileName}")
            {
                Title = "Release notes",
                Type = MediaType.Markdown
            };
            manifestLinks["release-notes-html"] = new HalLink($"https://github.com/dotnet/core/blob/main/release-notes/{patchDirPath}/{mdFileName}")
            {
                Title = "Release notes (HTML)",
                Type = MediaType.Html
            };
        }
        else if (File.Exists(readmePath))
        {
            manifestLinks["release-notes-markdown"] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/README.md")
            {
                Title = "Release notes",
                Type = MediaType.Markdown
            };
            manifestLinks["release-notes-html"] = new HalLink($"https://github.com/dotnet/core/blob/main/release-notes/{patchDirPath}/README.md")
            {
                Title = "Release notes (HTML)",
                Type = MediaType.Html
            };
        }

        // Add additional markdown files (component-specific release notes like aspnetcore.md, csharp.md, etc.)
        var mainMdFile = File.Exists(versionMdPath) ? $"{patchVersion}.md" : "README.md";
        var additionalMdFiles = Directory.GetFiles(patchDir, "*.md")
            .Select(Path.GetFileName)
            .Where(f => f != null && !f.Equals(mainMdFile, StringComparison.OrdinalIgnoreCase) && !f.Equals("README.md", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var mdFile in additionalMdFiles)
        {
            var baseName = Path.GetFileNameWithoutExtension(mdFile)!;

            // Determine relation name based on whether filename looks like a version
            // Version files (e.g., 9.0.111.md) -> release-notes-9.0.111-markdown
            // Component files (e.g., aspnetcore.md) -> whats-new-aspnetcore
            var relationName = IsVersionString(baseName)
                ? $"release-notes-{baseName.ToLowerInvariant()}-markdown"
                : $"whats-new-{baseName.ToLowerInvariant()}";

            // Extract H1 title from the markdown file, fall back to formatted filename
            var mdFilePath = Path.Combine(patchDir, mdFile!);
            var title = await ExtractMarkdownH1Async(mdFilePath) ?? FormatMarkdownTitle(baseName);

            manifestLinks[relationName] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/{mdFile}")
            {
                Title = title,
                Type = MediaType.Markdown
            };
        }

        // Add manifest link to index
        links[LinkRelations.Manifest] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/{FileNames.Manifest}")
        {
            Title = $"Manifest - .NET {majorVersion}",
        };

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
                        // Note: No titles or types in _embedded links - context established by parent
                        var bandLinks = new Dictionary<string, HalLink>
                        {
                            ["downloads"] = new HalLink($"{Location.GitHubBaseUri}{majorVersion}/{FileNames.Directories.Downloads}/sdk-{featureBand}.json")
                        };

                        // Add release-month link if we have lifecycle date
                        if (lifecycle?.GaDate != null)
                        {
                            var year = lifecycle.GaDate.Year.ToString("D4");
                            var month = lifecycle.GaDate.Month.ToString("D2");
                            var monthIndexPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Index}";
                            bandLinks["release-month"] = new HalLink($"{Location.GitHubBaseUri}{monthIndexPath}");
                        }

                        // Add release-patch link (to this patch release)
                        bandLinks["release-patch"] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/{FileNames.Index}");

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
        string? cveYear = null;
        string? cveMonth = null;

        if (lifecycle?.GaDate != null)
        {
            var releaseDate = lifecycle.GaDate;
            cveYear = releaseDate.Year.ToString("D4");
            cveMonth = releaseDate.Month.ToString("D2");
            var year = cveYear;
            var month = cveMonth;
            timelineCveJsonPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Cve}";
            timelineMonthIndexPath = $"{FileNames.Directories.Timeline}/{year}/{month}/{FileNames.Index}";

            // Add link to release month (HAL+JSON - added before JSON links)
            links[LinkRelations.Month] = new HalLink($"{Location.GitHubBaseUri}{timelineMonthIndexPath}")
            {
                Title = IndexTitles.TimelineMonthLink(year, month),
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

        // Add release-json to manifest if the file exists (older versions may not have it)
        if (File.Exists(releaseJsonPath))
        {
            manifestLinks["release-json"] = new HalLink($"{Location.GitHubBaseUri}{patchDirPath}/{FileNames.Release}")
            {
                Title = "Release information",
                Type = MediaType.Json
            };
        }

        // Add CVE JSON link if there are disclosures
        if (hasCveDisclosures && timelineCveJsonPath != null && cveYear != null && cveMonth != null)
        {
            var cveTitle = $"CVE records - {IndexTitles.FormatMonthYear(cveYear, cveMonth)}";
            links[LinkRelations.CveJson] = new HalLink($"{Location.GitHubBaseUri}{timelineCveJsonPath}")
            {
                Title = cveTitle,
                Type = MediaType.Json
            };

            // Add security-disclosures as semantic alias to month index (competes better with CVE/security queries)
            links[LinkRelations.SecurityDisclosures] = new HalLink($"{Location.GitHubBaseUri}{timelineMonthIndexPath}")
            {
                Title = $"Security disclosures - {IndexTitles.FormatMonthYear(cveYear, cveMonth)}",
            };

            // Add CVE markdown links to manifest (raw and rendered)
            // timelineCveJsonPath is like "timeline/2025/01/cve.json", change to "timeline/2025/01/cve.md"
            var timelineCveMdPath = timelineCveJsonPath.Replace("cve.json", "cve.md");
            manifestLinks["cve-markdown"] = new HalLink($"{Location.GitHubBaseUri}{timelineCveMdPath}")
            {
                Title = cveTitle,
                Type = MediaType.Markdown
            };
            manifestLinks["cve-html"] = new HalLink($"https://github.com/dotnet/core/blob/main/release-notes/{timelineCveMdPath}")
            {
                Title = $"CVE records (HTML) - {IndexTitles.FormatMonthYear(cveYear, cveMonth)}",
                Type = MediaType.Html
            };
        }

        // Build embedded content
        // Extract sorted CVE IDs from disclosures (source of truth from cve.json)
        IReadOnlyList<string>? sortedCveIds = null;
        if (cveDisclosures != null && cveDisclosures.Count > 0)
        {
            sortedCveIds = cveDisclosures.Select(d => d.Id).ToList();
        }

        // Note: CVE disclosures are intentionally omitted from patch detail embedded content.
        // CVEs are a timeline concept - use month or cve-json link for details.
        PatchDetailIndexEmbedded? embedded = null;
        if (sdkFeatureBandEntries != null)
        {
            embedded = new PatchDetailIndexEmbedded
            {
                Sdk = sdkFeatureBandEntries?.FirstOrDefault(),  // highest SDK (list is sorted descending)
                SdkFeatureBands = sdkFeatureBandEntries
            };
        }

        var patchDetailIndex = new PatchDetailIndex(
            ReleaseKind.Patch,
            $".NET Patch Release Index - {patchVersion}",
            patchVersion,
            lifecycle?.GaDate,
            lifecycle?.Phase,
            cveIds?.Count > 0,
            sortedCveIds)
        {
            PrevPatchDate = prevPatchDate,
            PrevSecurityPatchDate = prevSecurityPatchDate,
            SdkVersion = highestSdkVersion,
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

        // Read _manifest.json if it exists and merge links
        var partialManifestPath = Path.Combine(patchDir, FileNames.PartialManifest);
        if (File.Exists(partialManifestPath))
        {
            try
            {
                var partialJson = await File.ReadAllTextAsync(partialManifestPath);
                var partial = JsonSerializer.Deserialize<PartialContentManifest>(partialJson, ReleaseManifestSerializerContext.Default.PartialContentManifest);
                if (partial?.Links != null)
                {
                    foreach (var (key, link) in partial.Links)
                    {
                        if (key == HalTerms.Self)
                            continue;
                        manifestLinks[key] = link;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to read {partialManifestPath}: {ex.Message}");
            }
        }

        // Write manifest.json
        var manifest = new ContentManifest(
            "manifest",
            $"Manifest - .NET {patchVersion}")
        {
            Links = HalHelpers.OrderLinks(manifestLinks)
        };

        var manifestJson = JsonSerializer.Serialize(
            manifest,
            ReleaseManifestSerializerContext.Default.ContentManifest);

        // Add schema reference
        var manifestSchemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.ReleaseManifest}";
        var updatedManifestJson = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(manifestJson, manifestSchemaUri);

        var manifestPath = Path.Combine(outputPatchDir, FileNames.Manifest);
        await File.WriteAllTextAsync(manifestPath, (updatedManifestJson ?? manifestJson) + '\n');
    }

    /// <summary>
    /// Checks if a string looks like a version number (e.g., "9.0.111", "10.0.0-preview.1").
    /// </summary>
    private static bool IsVersionString(string name)
    {
        // Version strings start with a digit and contain at least one dot
        return !string.IsNullOrEmpty(name)
            && char.IsDigit(name[0])
            && name.Contains('.');
    }

    /// <summary>
    /// Extracts the H1 title from a markdown file.
    /// Returns null if no H1 is found.
    /// </summary>
    private static async Task<string?> ExtractMarkdownH1Async(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Check for ATX-style H1: # Title
                if (line.StartsWith("# "))
                {
                    return line[2..].Trim();
                }

                // If the first non-empty line isn't an H1, stop looking
                // (H1 should be at the top of the document)
                break;
            }
        }
        catch
        {
            // Ignore errors reading the file
        }

        return null;
    }

    /// <summary>
    /// Formats a markdown filename base into a human-readable title.
    /// </summary>
    private static string FormatMarkdownTitle(string baseName)
    {
        // Handle known component names with special formatting
        return baseName.ToLowerInvariant() switch
        {
            "aspnetcore" => "ASP.NET Core",
            "dotnetmaui" or "maui" => ".NET MAUI",
            "csharp" => "C#",
            "fsharp" => "F#",
            "vb" or "visualbasic" => "Visual Basic",
            "efcore" or "entityframeworkcore" => "Entity Framework Core",
            "wpf" => "WPF",
            "winforms" => "Windows Forms",
            "windowsdesktop" => "Windows Desktop",
            "sdk" => "SDK",
            "runtime" => "Runtime",
            "libraries" => "Libraries",
            "networking" => "Networking",
            "containers" => "Containers",
            "blazor" => "Blazor",
            "signalr" => "SignalR",
            "grpc" => "gRPC",
            "json" => "JSON",
            "xml" => "XML",
            "api" or "apis" => "APIs",
            _ => ToTitleCase(baseName)
        };
    }

    /// <summary>
    /// Converts a string to title case, handling camelCase and kebab-case.
    /// </summary>
    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Handle kebab-case: split on hyphens, title case each word
        if (input.Contains('-'))
        {
            return string.Join(" ", input.Split('-').Select(word =>
                char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
        }

        // Simple title case for single words
        return char.ToUpperInvariant(input[0]) + input[1..].ToLowerInvariant();
    }
}
