using System.Globalization;
using System.Text.Json;
using DotnetRelease;
using DotnetRelease.Graph;
using DotnetRelease.Summary;
using JsonSchemaInjector;
using CveHandler;

namespace VersionIndex;

public class SdkIndexFiles
{
    private static readonly List<string> SupportedSdkFiles = [
        "dotnet-sdk-linux-arm.tar.gz",
        "dotnet-sdk-linux-arm64.tar.gz", 
        "dotnet-sdk-linux-musl-arm.tar.gz",
        "dotnet-sdk-linux-musl-arm64.tar.gz",
        "dotnet-sdk-linux-musl-x64.tar.gz",
        "dotnet-sdk-linux-x64.tar.gz",
        "dotnet-sdk-osx-arm64.pkg",
        "dotnet-sdk-osx-arm64.tar.gz",
        "dotnet-sdk-osx-x64.pkg", 
        "dotnet-sdk-osx-x64.tar.gz",
        "dotnet-sdk-win-arm64.exe",
        "dotnet-sdk-win-arm64.zip",
        "dotnet-sdk-win-x64.exe",
        "dotnet-sdk-win-x64.zip",
        "dotnet-sdk-win-x86.exe",
        "dotnet-sdk-win-x86.zip"
    ];

    /// <summary>
    /// Generates SDK index files for all major versions that support SDK feature bands (8.0+)
    /// </summary>
    public static async Task GenerateAsync(List<MajorReleaseSummary> summaries, string rootDir)
    {
        if (!Directory.Exists(rootDir))
        {
            throw new DirectoryNotFoundException($"Root directory does not exist: {rootDir}");
        }

        var urlGenerator = (string relativePath, LinkStyle style) => style == LinkStyle.Prod
            ? $"{Location.GitHubBaseUri}{relativePath}"
            : LinkHelpers.GetGitHubPath(relativePath);

        var halLinkGenerator = new HalLinkGenerator(rootDir, urlGenerator);

        foreach (var summary in summaries)
        {
            // Only generate SDK indexes for .NET 8.0 and later as per specification
            if (!IsVersionSupported(summary.MajorVersion))
            {
                continue;
            }

            var majorVersionDir = Path.Combine(rootDir, summary.MajorVersion);
            if (!Directory.Exists(majorVersionDir))
            {
                continue;
            }

            Console.WriteLine($"Generating SDK indexes for .NET {summary.MajorVersion}");

            await GenerateSdkIndexForMajorVersion(summary, majorVersionDir, halLinkGenerator);
        }
    }

    private static bool IsVersionSupported(string version)
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

    private static async Task GenerateSdkIndexForMajorVersion(MajorReleaseSummary summary, string majorVersionDir, HalLinkGenerator halLinkGenerator)
    {
        var sdkDir = Path.Combine(majorVersionDir, FileNames.Directories.Sdk);
        Directory.CreateDirectory(sdkDir);

        // Generate main SDK index for the major version
        await GenerateSdkMainIndex(summary, sdkDir, halLinkGenerator);

        // Generate feature band indexes
        await GenerateFeatureBandIndexes(summary, sdkDir, halLinkGenerator);
    }

    private static async Task GenerateSdkMainIndex(MajorReleaseSummary summary, string sdkDir, HalLinkGenerator halLinkGenerator)
    {
        var indexPath = Path.Combine(sdkDir, FileNames.Index);
        var rootDir = Path.GetDirectoryName(Path.GetDirectoryName(sdkDir)) ?? throw new InvalidOperationException("Unable to determine root directory");

        // Pre-load CVE records by month to avoid repeated file reads
        var cveRecordsByMonth = new Dictionary<string, DotnetRelease.Security.CveRecords?>();
        var indexRelativePath = Path.GetRelativePath(rootDir, indexPath);
        var indexPathValue = "/" + indexRelativePath.Replace("\\", "/");

        // Downloads file path
        var downloadsFileName = $"sdk-{summary.MajorVersion}.json";
        var downloadsRelativePath = $"{summary.MajorVersion}/{FileNames.Directories.Sdk}/{downloadsFileName}";

        // Create main links
        var links = new Dictionary<string, HalLink>
        {
            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{indexRelativePath}")
            {
                Path = indexPathValue,
                Title = $".NET SDK {summary.MajorVersion}",
                Type = MediaType.HalJson
            },
            [LinkRelations.ReleaseMajor] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Index}")
            {
                Path = $"/{summary.MajorVersion}/{FileNames.Index}",
                Title = $".NET {summary.MajorVersion}",
                Type = MediaType.HalJson
            },
            ["downloads"] = new HalLink($"{Location.GitHubBaseUri}{downloadsRelativePath}")
            {
                Path = $"/{downloadsRelativePath}",
                Title = $".NET SDK {summary.MajorVersion} Downloads",
                Type = MediaType.Json
            }
        };

        // Create feature band entries (first embedded section)
        var featureBandEntries = new List<SdkFeatureBandEntry>();

        foreach (var sdkBand in summary.SdkBands)
        {
            var bandVersion = sdkBand.Version[..5] + "xx"; // e.g., "8.0.1xx"

            var bandFileName = $"sdk-{bandVersion}.json";
            var bandFilePath = Path.Combine(sdkDir, bandFileName);
            var bandRelativePath = Path.GetRelativePath(rootDir, bandFilePath);
            var bandPathValue = "/" + bandRelativePath.Replace("\\", "/");

            var bandLinks = new Dictionary<string, HalLink>
            {
                [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{bandRelativePath}")
                {
                    Path = bandPathValue,
                    Title = $".NET SDK {bandVersion}",
                    Type = MediaType.Json
                }
            };

            var featureBandEntry = new SdkFeatureBandEntry(
                bandVersion,
                new DateTimeOffset(sdkBand.LatestReleaseDate.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-8)),
                $".NET SDK {bandVersion}",
                sdkBand.SupportPhase,
                bandLinks);

            featureBandEntries.Add(featureBandEntry);
        }

        // Create SDK patch release entries (second embedded section)
        var sdkReleaseEntries = new List<SdkReleaseEntry>();
        var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);

        // Collect all SDK components first, then sort by SDK version
        var sdkComponents = new List<(PatchReleaseSummary PatchRelease, ReleaseComponent SdkComponent)>();

        foreach (var patchRelease in summary.PatchReleases)
        {
            foreach (var component in patchRelease.Components)
            {
                if (component.Name.Equals("sdk", StringComparison.OrdinalIgnoreCase))
                {
                    sdkComponents.Add((patchRelease, component));
                }
            }
        }

        // Sort SDK components by SDK version descending (newest first)
        var sortedSdkComponents = sdkComponents
            .OrderByDescending(sdk => sdk.SdkComponent.Version, numericStringComparer)
            .ToList();

        foreach (var (patchRelease, component) in sortedSdkComponents)
        {
            // Link to patch detail index.json file
            var indexRelPath = $"{summary.MajorVersion}/{patchRelease.PatchVersion}/{FileNames.Index}";
            var releaseLinks = new Dictionary<string, HalLink>
            {
                [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{indexRelPath}")
                {
                    Path = $"/{indexRelPath}",
                    Title = $"{patchRelease.PatchVersion}",
                    Type = MediaType.HalJson
                }
            };

            // Get CVE IDs - prefer cve.json (authoritative), fall back to releases.json
            IReadOnlyList<string>? cveIds = null;

            var releaseDate = patchRelease.ReleaseDate;
            var monthKey = $"{releaseDate.Year:D4}-{releaseDate.Month:D2}";

            // Load CVE records for this month if not already cached
            if (!cveRecordsByMonth.TryGetValue(monthKey, out var cveRecords))
            {
                var releaseDateOffset = new DateTimeOffset(releaseDate.Year, releaseDate.Month, releaseDate.Day, 0, 0, 0, TimeSpan.Zero);
                cveRecords = await CveLoader.LoadCveRecordsForReleaseDateAsync(rootDir, releaseDateOffset);
                cveRecordsByMonth[monthKey] = cveRecords;
            }

            // Try to get CVE IDs from cve.json (authoritative source)
            if (cveRecords?.ReleaseCves != null && cveRecords.ReleaseCves.TryGetValue(summary.MajorVersion, out var cveIdsFromCveJson))
            {
                cveIds = cveIdsFromCveJson.ToList();
            }
            else if (patchRelease.Security && patchRelease.CveList?.Count > 0)
            {
                // Fall back to releases.json if cve.json not available
                cveIds = patchRelease.CveList.Select(cve => cve.CveId).ToList();
            }

            var sdkReleaseEntry = new SdkReleaseEntry(
                component.Version,
                new DateTimeOffset(patchRelease.ReleaseDate.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-8)),
                patchRelease.Security,
                SupportPhase.Active,
                releaseLinks)
            {
                CveRecords = cveIds
            };

            sdkReleaseEntries.Add(sdkReleaseEntry);
        }

        // Determine latest and latest-security SDK versions
        var latestSdk = sdkReleaseEntries.FirstOrDefault()?.Version;
        var latestSecuritySdk = sdkReleaseEntries.FirstOrDefault(e => e.Security)?.Version;

        // Create the main SDK index (no downloads - those go in separate file)
        var sdkIndex = new SdkVersionIndex(
            ReleaseKind.SdkIndex,
            summary.MajorVersion,
            $".NET SDK {summary.MajorVersion} Index",
            $"SDK release index for .NET {summary.MajorVersion}")
        {
            Latest = latestSdk,
            LatestSecurity = latestSecuritySdk,
            Links = HalHelpers.OrderLinks(links),
            Embedded = new SdkVersionIndexEmbedded(sdkReleaseEntries, featureBandEntries),
            Metadata = new GenerationMetadata("1.0", DateTimeOffset.UtcNow, "VersionIndex")
        };

        // Serialize to JSON
        var json = JsonSerializer.Serialize(
            sdkIndex,
            SdkVersionIndexSerializerContext.Default.SdkVersionIndex);

        // Add schema reference
        var schemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.SdkVersionIndex}";
        var jsonWithSchema = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(json, schemaUri);

        await File.WriteAllTextAsync(indexPath, jsonWithSchema);

        // Generate the major version downloads file (sdk-9.0.json)
        await GenerateMajorVersionDownloadsFile(summary, sdkDir);
    }

    private static async Task GenerateMajorVersionDownloadsFile(MajorReleaseSummary summary, string sdkDir)
    {
        var fileName = $"sdk-{summary.MajorVersion}.json";
        var filePath = Path.Combine(sdkDir, fileName);

        var sdkFilesDict = GenerateSdkFilesDictionary(summary.MajorVersion);

        // Get the latest active band for support phase
        var latestBand = summary.SdkBands
            .Where(b => b.SupportPhase == SupportPhase.Active)
            .OrderByDescending(b => b.LatestReleaseDate)
            .FirstOrDefault() ?? summary.SdkBands.LastOrDefault();

        var links = new Dictionary<string, HalLink>
        {
            ["self"] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Directories.Sdk}/{fileName}")
            {
                Path = $"/{summary.MajorVersion}/{FileNames.Directories.Sdk}/{fileName}",
                Title = $".NET SDK {summary.MajorVersion} Downloads",
                Type = MediaType.Json
            },
            ["sdk-index"] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Directories.Sdk}/{FileNames.Index}")
            {
                Path = $"/{summary.MajorVersion}/{FileNames.Directories.Sdk}/{FileNames.Index}",
                Title = $".NET SDK {summary.MajorVersion}",
                Type = MediaType.HalJson
            },
            [LinkRelations.ReleaseMajor] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Index}")
            {
                Path = $"/{summary.MajorVersion}/{FileNames.Index}",
                Title = $".NET {summary.MajorVersion}",
                Type = MediaType.HalJson
            }
        };

        var sdkDownloadInfo = new SdkDownloadInfo(
            ReleaseKind.SdkDownload,
            summary.MajorVersion,
            latestBand?.SupportPhase,
            $".NET SDK {summary.MajorVersion} Downloads",
            $"SDK downloads for .NET {summary.MajorVersion} (latest feature band)",
            links)
        {
            Embedded = new SdkDownloadEmbedded(sdkFilesDict)
        };

        var json = JsonSerializer.Serialize(
            sdkDownloadInfo,
            SdkVersionIndexSerializerContext.Default.SdkDownloadInfo);

        // Add schema reference
        var schemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.SdkDownload}";
        var jsonWithSchema = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(json, schemaUri);

        await File.WriteAllTextAsync(filePath, jsonWithSchema ?? json);
    }

    private static async Task GenerateFeatureBandIndexes(MajorReleaseSummary summary, string sdkDir, HalLinkGenerator halLinkGenerator)
    {
        foreach (var sdkBand in summary.SdkBands)
        {
            var bandVersion = sdkBand.Version[..5]; // e.g., "8.0.1"
            var bandXX = bandVersion + "xx"; // e.g., "8.0.1xx"
            var fileName = $"sdk-{bandXX}.json";
            var filePath = Path.Combine(sdkDir, fileName);

            var sdkFilesDict = GenerateSdkFilesDictionary(bandXX);

            var links = new Dictionary<string, HalLink>
            {
                ["self"] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Directories.Sdk}/{fileName}")
                {
                    Path = $"/{summary.MajorVersion}/{FileNames.Directories.Sdk}/{fileName}",
                    Title = $".NET SDK {bandXX} Downloads",
                    Type = MediaType.Json
                },
                ["sdk-index"] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Directories.Sdk}/{FileNames.Index}")
                {
                    Path = $"/{summary.MajorVersion}/{FileNames.Directories.Sdk}/{FileNames.Index}",
                    Title = $".NET SDK {summary.MajorVersion}",
                    Type = MediaType.HalJson
                },
                [LinkRelations.ReleaseMajor] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Index}")
                {
                    Path = $"/{summary.MajorVersion}/{FileNames.Index}",
                    Title = $".NET {summary.MajorVersion}",
                    Type = MediaType.HalJson
                }
            };

            var sdkDownloadInfo = new SdkDownloadInfo(
                ReleaseKind.SdkDownload,
                bandXX,
                sdkBand.SupportPhase,
                $".NET SDK {bandXX} Downloads",
                $"SDK downloads for .NET SDK {bandXX} feature band",
                links)
            {
                Embedded = new SdkDownloadEmbedded(sdkFilesDict)
            };

            var json = JsonSerializer.Serialize(
                sdkDownloadInfo,
                SdkVersionIndexSerializerContext.Default.SdkDownloadInfo);

            // Add schema reference
            var schemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.SdkDownload}";
            var jsonWithSchema = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(json, schemaUri);

            await File.WriteAllTextAsync(filePath, jsonWithSchema ?? json);
        }
    }

    private static Dictionary<string, SdkDownloadFile> GenerateSdkFilesDictionary(string version)
    {
        var downloads = new Dictionary<string, SdkDownloadFile>();

        foreach (var fileName in SupportedSdkFiles)
        {
            var platformInfo = ParseFileNameForPlatform(fileName);

            var links = new Dictionary<string, HalLink>
            {
                ["download"] = new HalLink($"https://aka.ms/dotnet/{version}/{fileName}")
                {
                    Title = $"Download {fileName}"
                },
                ["hash"] = new HalLink($"https://aka.ms/dotnet/{version}/{fileName}.sha512")
                {
                    Title = "SHA512 hash file"
                }
            };

            var downloadFile = new SdkDownloadFile(
                fileName,
                platformInfo.Rid,
                platformInfo.Os,
                platformInfo.Arch,
                "sha512",
                links);

            downloads[platformInfo.Rid] = downloadFile;
        }

        return downloads;
    }

    private static (string Type, string Rid, string Os, string Arch) ParseFileNameForPlatform(string fileName)
    {
        // Extract platform info from filename patterns
        var extension = Path.GetExtension(fileName).TrimStart('.');
        var type = extension == "gz" ? "tar.gz" : extension;
        
        var nameWithoutExt = fileName.Replace(".tar.gz", "").Replace($".{extension}", "");
        var parts = nameWithoutExt.Split('-');

        if (parts.Length >= 3)
        {
            var os = parts[2];
            var arch = parts.Length > 3 ? parts[3] : "x64";
            
            // Handle special cases
            if (os == "musl")
            {
                os = "linux-musl";
                arch = parts.Length > 4 ? parts[4] : parts[3];
            }

            var rid = $"{os}-{arch}";
            return (type, rid, os, arch);
        }

        return (type, "unknown", "unknown", "unknown");
    }


    private static PatchLifecycle CreatePatchLifecycle(SupportPhase phase, DateOnly releaseDate)
    {
        // Create patch lifecycle with only phase and release-date per spec
        var releaseDateTime = new DateTimeOffset(releaseDate.ToDateTime(TimeOnly.MinValue));
        
        return new PatchLifecycle(phase, releaseDateTime);
    }

}