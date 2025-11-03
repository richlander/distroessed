using System.Globalization;
using System.Text.Json;
using DotnetRelease;
using JsonSchemaInjector;

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
        var sdkDir = Path.Combine(majorVersionDir, "sdk");
        Directory.CreateDirectory(sdkDir);

        // Generate main SDK index for the major version
        await GenerateSdkMainIndex(summary, sdkDir, halLinkGenerator);

        // Generate feature band indexes
        await GenerateFeatureBandIndexes(summary, sdkDir, halLinkGenerator);
    }

    private static async Task GenerateSdkMainIndex(MajorReleaseSummary summary, string sdkDir, HalLinkGenerator halLinkGenerator)
    {
        var indexPath = Path.Combine(sdkDir, "index.json");
        var rootDir = Path.GetDirectoryName(Path.GetDirectoryName(sdkDir)) ?? throw new InvalidOperationException("Unable to determine root directory");
        var indexRelativePath = Path.GetRelativePath(rootDir, indexPath);
        
        // Create main links
        var links = new Dictionary<string, HalLink>
        {
            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{indexRelativePath}")
            {
                Relative = indexRelativePath,
                Title = $".NET SDK {summary.MajorVersion}",
                Type = MediaType.HalJson
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
            
            var bandLinks = new Dictionary<string, HalLink>
            {
                [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{bandRelativePath}")
                {
                    Relative = bandRelativePath,
                    Title = $".NET SDK {bandVersion}",
                    Type = MediaType.Json
                }
            };

            // Create patch lifecycle for feature band (no release-type)
            var bandLifecycle = CreatePatchLifecycle(sdkBand.SupportPhase, sdkBand.LatestReleaseDate);

            var featureBandEntry = new SdkFeatureBandEntry(
                ReleaseKind.Band,
                bandVersion,
                $".NET SDK {bandVersion}",
                bandLinks)
            {
                Lifecycle = bandLifecycle
            };

            featureBandEntries.Add(featureBandEntry);
        }

        // Create SDK patch release entries (second embedded section)
        var sdkReleaseEntries = new List<ReleaseVersionIndexEntry>();
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
            var releaseLinks = new Dictionary<string, HalLink>();
            
            // Link to runtime release.json file
            if (!string.IsNullOrEmpty(patchRelease.ReleaseJsonPath))
            {
                // Use the ReleaseJsonPath directly since it's already the correct relative path for URLs
                var releaseJsonRelativePath = patchRelease.ReleaseJsonPath;
                releaseLinks[HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{releaseJsonRelativePath}")
                {
                    Relative = releaseJsonRelativePath,
                    Title = $"{patchRelease.PatchVersion} Release Information",
                    Type = MediaType.Json
                };
            }

            // Link to SDK-specific markdown if available, otherwise runtime markdown
            var sdkMarkdownPath = Path.Combine(rootDir, summary.MajorVersion, patchRelease.PatchVersion, $"{component.Version}.md");
            var runtimeMarkdownPath = Path.Combine(rootDir, summary.MajorVersion, patchRelease.PatchVersion, $"{patchRelease.PatchVersion}.md");
            
            string markdownPath;
            if (File.Exists(sdkMarkdownPath))
            {
                markdownPath = sdkMarkdownPath;
            }
            else if (File.Exists(runtimeMarkdownPath))
            {
                markdownPath = runtimeMarkdownPath;
            }
            else
            {
                markdownPath = runtimeMarkdownPath; // Use expected path even if file doesn't exist
            }
            
            var markdownRelativePath = Path.GetRelativePath(rootDir, markdownPath);
            releaseLinks["release-notes-markdown"] = new HalLink($"{Location.GitHubBaseUri}{markdownRelativePath}")
            {
                Relative = markdownRelativePath,
                Title = $"Release Notes",
                Type = MediaType.Markdown
            };

            // Create patch lifecycle (no release-type)
            var patchLifecycle = CreatePatchLifecycle(SupportPhase.Active, patchRelease.ReleaseDate);

            var sdkReleaseEntry = new ReleaseVersionIndexEntry(
                component.Version,
                ReleaseKind.PatchRelease,
                releaseLinks)
            {
                Lifecycle = new PatchLifecycle(patchLifecycle.Phase, patchLifecycle.ReleaseDate)
            };

            sdkReleaseEntries.Add(sdkReleaseEntry);
        }

        // Generate latest SDK downloads dictionary
        var latestBand = summary.SdkBands
            .Where(b => b.SupportPhase == SupportPhase.Active)
            .OrderByDescending(b => b.LatestReleaseDate)
            .FirstOrDefault() ?? summary.SdkBands.LastOrDefault();

        Dictionary<string, SdkDownloadFile>? latestDownloads = null;
        if (latestBand != null)
        {
            latestDownloads = GenerateSdkFilesDictionary(summary.MajorVersion);
        }

        // Create the main SDK index with both embedded sections
        var sdkIndex = new SdkVersionIndex(
            ReleaseKind.Index,
            "sdk",
            summary.MajorVersion,
            $".NET SDK {summary.MajorVersion}",
            links)
        {
            Embedded = new SdkVersionIndexEmbedded(latestDownloads, featureBandEntries, sdkReleaseEntries),
            Metadata = new GenerationMetadata(DateTimeOffset.UtcNow, "UpdateIndexes")
        };

        // Serialize to JSON
        var json = JsonSerializer.Serialize(
            sdkIndex, 
            SdkVersionIndexSerializerContext.Default.SdkVersionIndex);

        // Add schema reference
        var schemaUri = $"{Location.GitHubBaseUri}schemas/dotnet-sdk-version-index.json";
        var jsonWithSchema = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(json, schemaUri);

        await File.WriteAllTextAsync(indexPath, jsonWithSchema);
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
                ["self"] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/sdk/{fileName}")
                {
                    Relative = $"{summary.MajorVersion}/sdk/{fileName}",
                    Title = $".NET SDK {bandXX} Downloads",
                    Type = MediaType.Json
                }
            };

            var sdkDownloadInfo = new SdkDownloadInfo(
                "sdk",
                bandXX,
                $".NET SDK {bandXX}",
                "sha512",
                links)
            {
                Embedded = new SdkDownloadEmbedded(sdkFilesDict)
            };

            var json = JsonSerializer.Serialize(
                sdkDownloadInfo,
                SdkVersionIndexSerializerContext.Default.SdkDownloadInfo);

            await File.WriteAllTextAsync(filePath, json);
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