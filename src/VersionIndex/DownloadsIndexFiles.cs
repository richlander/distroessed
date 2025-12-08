using System.Globalization;
using System.Text.Json;
using DotnetRelease;
using DotnetRelease.Graph;
using DotnetRelease.Summary;
using JsonSchemaInjector;

namespace VersionIndex;

/// <summary>
/// Generates the downloads/ directory with component download files for each major version.
/// </summary>
public class DownloadsIndexFiles
{
    // Runtime download files (cross-platform)
    private static readonly List<(string FileName, string Rid, string Os, string Arch)> RuntimeDownloads =
    [
        ("dotnet-runtime-linux-arm.tar.gz", "linux-arm", "linux", "arm"),
        ("dotnet-runtime-linux-arm64.tar.gz", "linux-arm64", "linux", "arm64"),
        ("dotnet-runtime-linux-musl-arm.tar.gz", "linux-musl-arm", "linux-musl", "arm"),
        ("dotnet-runtime-linux-musl-arm64.tar.gz", "linux-musl-arm64", "linux-musl", "arm64"),
        ("dotnet-runtime-linux-musl-x64.tar.gz", "linux-musl-x64", "linux-musl", "x64"),
        ("dotnet-runtime-linux-x64.tar.gz", "linux-x64", "linux", "x64"),
        ("dotnet-runtime-osx-arm64.tar.gz", "osx-arm64", "osx", "arm64"),
        ("dotnet-runtime-osx-x64.tar.gz", "osx-x64", "osx", "x64"),
        ("dotnet-runtime-win-arm64.zip", "win-arm64", "win", "arm64"),
        ("dotnet-runtime-win-x64.zip", "win-x64", "win", "x64"),
        ("dotnet-runtime-win-x86.zip", "win-x86", "win", "x86"),
    ];

    // ASP.NET Core Runtime download files (cross-platform)
    private static readonly List<(string FileName, string Rid, string Os, string Arch)> AspNetCoreDownloads =
    [
        ("aspnetcore-runtime-linux-arm.tar.gz", "linux-arm", "linux", "arm"),
        ("aspnetcore-runtime-linux-arm64.tar.gz", "linux-arm64", "linux", "arm64"),
        ("aspnetcore-runtime-linux-musl-arm.tar.gz", "linux-musl-arm", "linux-musl", "arm"),
        ("aspnetcore-runtime-linux-musl-arm64.tar.gz", "linux-musl-arm64", "linux-musl", "arm64"),
        ("aspnetcore-runtime-linux-musl-x64.tar.gz", "linux-musl-x64", "linux-musl", "x64"),
        ("aspnetcore-runtime-linux-x64.tar.gz", "linux-x64", "linux", "x64"),
        ("aspnetcore-runtime-osx-arm64.tar.gz", "osx-arm64", "osx", "arm64"),
        ("aspnetcore-runtime-osx-x64.tar.gz", "osx-x64", "osx", "x64"),
        ("aspnetcore-runtime-win-arm64.zip", "win-arm64", "win", "arm64"),
        ("aspnetcore-runtime-win-x64.zip", "win-x64", "win", "x64"),
        ("aspnetcore-runtime-win-x86.zip", "win-x86", "win", "x86"),
    ];

    // Windows Desktop Runtime download files (Windows only)
    private static readonly List<(string FileName, string Rid, string Os, string Arch)> WindowsDesktopDownloads =
    [
        ("windowsdesktop-runtime-win-arm64.exe", "win-arm64", "win", "arm64"),
        ("windowsdesktop-runtime-win-x64.exe", "win-x64", "win", "x64"),
        ("windowsdesktop-runtime-win-x86.exe", "win-x86", "win", "x86"),
    ];

    // SDK download files (cross-platform)
    private static readonly List<(string FileName, string Rid, string Os, string Arch)> SdkDownloads =
    [
        ("dotnet-sdk-linux-arm.tar.gz", "linux-arm", "linux", "arm"),
        ("dotnet-sdk-linux-arm64.tar.gz", "linux-arm64", "linux", "arm64"),
        ("dotnet-sdk-linux-musl-arm.tar.gz", "linux-musl-arm", "linux-musl", "arm"),
        ("dotnet-sdk-linux-musl-arm64.tar.gz", "linux-musl-arm64", "linux-musl", "arm64"),
        ("dotnet-sdk-linux-musl-x64.tar.gz", "linux-musl-x64", "linux-musl", "x64"),
        ("dotnet-sdk-linux-x64.tar.gz", "linux-x64", "linux", "x64"),
        ("dotnet-sdk-osx-arm64.tar.gz", "osx-arm64", "osx", "arm64"),
        ("dotnet-sdk-osx-x64.tar.gz", "osx-x64", "osx", "x64"),
        ("dotnet-sdk-win-arm64.zip", "win-arm64", "win", "arm64"),
        ("dotnet-sdk-win-x64.zip", "win-x64", "win", "x64"),
        ("dotnet-sdk-win-x86.zip", "win-x86", "win", "x86"),
    ];

    /// <summary>
    /// Generates downloads directory files for all major versions that support it (8.0+).
    /// </summary>
    public static async Task GenerateAsync(List<MajorReleaseSummary> summaries, string rootDir)
    {
        if (!Directory.Exists(rootDir))
        {
            throw new DirectoryNotFoundException($"Root directory does not exist: {rootDir}");
        }

        var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);

        foreach (var summary in summaries)
        {
            // Only generate downloads for .NET 8.0 and later
            if (!IsVersionSupported(summary.MajorVersion))
            {
                continue;
            }

            var majorVersionDir = Path.Combine(rootDir, summary.MajorVersion);
            if (!Directory.Exists(majorVersionDir))
            {
                continue;
            }

            Console.WriteLine($"Generating downloads directory for .NET {summary.MajorVersion}");

            var downloadsDir = Path.Combine(majorVersionDir, FileNames.Directories.Downloads);
            Directory.CreateDirectory(downloadsDir);

            // Generate component download files
            await GenerateRuntimeDownloadAsync(summary, downloadsDir, rootDir);
            await GenerateAspNetCoreDownloadAsync(summary, downloadsDir, rootDir);
            await GenerateWindowsDesktopDownloadAsync(summary, downloadsDir, rootDir);
            await GenerateSdkDownloadAsync(summary, downloadsDir, rootDir);

            // Generate feature band download files
            await GenerateFeatureBandDownloadsAsync(summary, downloadsDir, rootDir, numericStringComparer);

            // Generate downloads index
            await GenerateDownloadsIndexAsync(summary, downloadsDir, rootDir, numericStringComparer);
        }
    }

    private static bool IsVersionSupported(string version)
    {
        // Downloads directory is only supported for .NET 8.0 and later
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

    private static async Task GenerateDownloadsIndexAsync(
        MajorReleaseSummary summary,
        string downloadsDir,
        string rootDir,
        StringComparer numericStringComparer)
    {
        var indexPath = Path.Combine(downloadsDir, FileNames.Index);
        var indexRelativePath = $"{summary.MajorVersion}/{FileNames.Directories.Downloads}/{FileNames.Index}";

        var links = new Dictionary<string, HalLink>
        {
            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{indexRelativePath}")
            {
                Title = $".NET {summary.MajorVersion} Downloads",
            },
            [LinkRelations.ReleaseMajor] = new HalLink($"{Location.GitHubBaseUri}{summary.MajorVersion}/{FileNames.Index}")
            {
                Title = $".NET {summary.MajorVersion}",
            }
        };

        // Build component entries
        var components = new List<ComponentEntry>
        {
            CreateComponentEntry("runtime", ".NET Runtime", summary.MajorVersion),
            CreateComponentEntry("aspnetcore", "ASP.NET Core Runtime", summary.MajorVersion),
            CreateComponentEntry("windowsdesktop", "Windows Desktop Runtime", summary.MajorVersion),
            CreateSdkComponentEntry(summary.MajorVersion),
        };

        // Build feature band entries (sorted by version descending)
        var featureBands = summary.SdkBands
            .OrderByDescending(b => b.Version, numericStringComparer)
            .Select(band =>
            {
                var bandVersion = band.Version[..5] + "xx"; // e.g., "8.0.4xx"
                var bandFileName = $"sdk-{bandVersion}.json";
                var bandRelativePath = $"{summary.MajorVersion}/{FileNames.Directories.Downloads}/{bandFileName}";

                var bandLinks = new Dictionary<string, HalLink>
                {
                    [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{bandRelativePath}")
                    {
                        Title = $".NET SDK {bandVersion}",
                        Type = MediaType.Json
                    }
                };

                return new FeatureBandEntry(bandVersion, $".NET SDK {bandVersion}", band.SupportPhase)
                {
                    Links = bandLinks
                };
            })
            .ToList();

        var downloadsIndex = new DownloadsIndex(
            ReleaseKind.DownloadsIndex,
            summary.MajorVersion,
            $".NET {summary.MajorVersion} Downloads")
        {
            Links = HalHelpers.OrderLinks(links),
            Embedded = new DownloadsIndexEmbedded
            {
                Components = components,
                FeatureBands = featureBands.Count > 0 ? featureBands : null
            }
        };

        var json = JsonSerializer.Serialize(
            downloadsIndex,
            DownloadsIndexSerializerContext.Default.DownloadsIndex);

        var schemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.DownloadsIndex}";
        var jsonWithSchema = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(json, schemaUri);

        await File.WriteAllTextAsync(indexPath, (jsonWithSchema ?? json) + '\n');
    }

    private static ComponentEntry CreateComponentEntry(string name, string title, string version)
    {
        var fileName = $"{name}.json";
        var relativePath = $"{version}/{FileNames.Directories.Downloads}/{fileName}";

        var links = new Dictionary<string, HalLink>
        {
            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{relativePath}")
            {
                Title = title,
                Type = MediaType.Json
            }
        };

        return new ComponentEntry(name, title) { Links = links };
    }

    private static ComponentEntry CreateSdkComponentEntry(string version)
    {
        var fileName = "sdk.json";
        var relativePath = $"{version}/{FileNames.Directories.Downloads}/{fileName}";
        var sdkIndexPath = $"{version}/{FileNames.Directories.Sdk}/{FileNames.Index}";

        var links = new Dictionary<string, HalLink>
        {
            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{relativePath}")
            {
                Title = ".NET SDK",
                Type = MediaType.Json
            },
            ["sdk-index"] = new HalLink($"{Location.GitHubBaseUri}{sdkIndexPath}")
            {
                Title = $".NET SDK {version}",
            }
        };

        return new ComponentEntry("sdk", ".NET SDK") { Links = links };
    }

    private static async Task GenerateRuntimeDownloadAsync(MajorReleaseSummary summary, string downloadsDir, string rootDir)
    {
        await GenerateComponentDownloadAsync(
            summary.MajorVersion,
            "runtime",
            ".NET Runtime",
            $"Evergreen download links for .NET Runtime {summary.MajorVersion}",
            RuntimeDownloads,
            "dotnet-runtime",
            downloadsDir,
            rootDir);
    }

    private static async Task GenerateAspNetCoreDownloadAsync(MajorReleaseSummary summary, string downloadsDir, string rootDir)
    {
        await GenerateComponentDownloadAsync(
            summary.MajorVersion,
            "aspnetcore",
            "ASP.NET Core Runtime",
            $"Evergreen download links for ASP.NET Core Runtime {summary.MajorVersion}",
            AspNetCoreDownloads,
            "aspnetcore-runtime",
            downloadsDir,
            rootDir);
    }

    private static async Task GenerateWindowsDesktopDownloadAsync(MajorReleaseSummary summary, string downloadsDir, string rootDir)
    {
        await GenerateComponentDownloadAsync(
            summary.MajorVersion,
            "windowsdesktop",
            "Windows Desktop Runtime",
            $"Evergreen download links for Windows Desktop Runtime {summary.MajorVersion}",
            WindowsDesktopDownloads,
            "windowsdesktop-runtime",
            downloadsDir,
            rootDir);
    }

    private static async Task GenerateSdkDownloadAsync(MajorReleaseSummary summary, string downloadsDir, string rootDir)
    {
        // sdk.json uses major version URLs (aka.ms/dotnet/8.0/...)
        await GenerateComponentDownloadAsync(
            summary.MajorVersion,
            "sdk",
            ".NET SDK",
            $"Evergreen download links for .NET SDK {summary.MajorVersion} (latest feature band)",
            SdkDownloads,
            "dotnet-sdk",
            downloadsDir,
            rootDir,
            featureBand: null,
            includeSdkIndexLink: true);
    }

    private static async Task GenerateFeatureBandDownloadsAsync(
        MajorReleaseSummary summary,
        string downloadsDir,
        string rootDir,
        StringComparer numericStringComparer)
    {
        foreach (var band in summary.SdkBands)
        {
            var bandVersion = band.Version[..5] + "xx"; // e.g., "8.0.4xx"
            var fileName = $"sdk-{bandVersion}.json";
            var filePath = Path.Combine(downloadsDir, fileName);

            await GenerateComponentDownloadAsync(
                summary.MajorVersion,
                "sdk",
                $".NET SDK {bandVersion}",
                $"Evergreen download links for .NET SDK {bandVersion} feature band",
                SdkDownloads,
                "dotnet-sdk",
                downloadsDir,
                rootDir,
                featureBand: bandVersion,
                includeSdkIndexLink: true,
                customFileName: fileName);
        }
    }

    private static async Task GenerateComponentDownloadAsync(
        string version,
        string component,
        string title,
        string description,
        List<(string FileName, string Rid, string Os, string Arch)> downloads,
        string filePrefix,
        string downloadsDir,
        string rootDir,
        string? featureBand = null,
        bool includeSdkIndexLink = false,
        string? customFileName = null)
    {
        var fileName = customFileName ?? $"{component}.json";
        var filePath = Path.Combine(downloadsDir, fileName);
        var relativePath = $"{version}/{FileNames.Directories.Downloads}/{fileName}";
        var downloadVersion = featureBand ?? version; // Use feature band for SDK band files, version otherwise

        var links = new Dictionary<string, HalLink>
        {
            [HalTerms.Self] = new HalLink($"{Location.GitHubBaseUri}{relativePath}")
            {
                Title = $"{title} Downloads",
                Type = MediaType.Json
            },
            ["downloads-index"] = new HalLink($"{Location.GitHubBaseUri}{version}/{FileNames.Directories.Downloads}/{FileNames.Index}")
            {
                Title = $".NET {version} Downloads",
            },
            [LinkRelations.ReleaseMajor] = new HalLink($"{Location.GitHubBaseUri}{version}/{FileNames.Index}")
            {
                Title = $".NET {version}",
            }
        };

        if (includeSdkIndexLink)
        {
            links["sdk-index"] = new HalLink($"{Location.GitHubBaseUri}{version}/{FileNames.Directories.Sdk}/{FileNames.Index}")
            {
                Title = $".NET SDK {version}",
            };
        }

        // Build download file entries
        var downloadFiles = new Dictionary<string, DownloadFile>();
        foreach (var (fn, rid, os, arch) in downloads)
        {
            var downloadLinks = new Dictionary<string, HalLink>
            {
                ["download"] = new HalLink($"https://aka.ms/dotnet/{downloadVersion}/{fn}")
                {
                    Title = $"Download {fn}"
                },
                ["hash"] = new HalLink($"https://aka.ms/dotnet/{downloadVersion}/{fn}.sha512")
                {
                    Title = "SHA512 hash file"
                }
            };

            downloadFiles[rid] = new DownloadFile(fn, rid, os, arch, "sha512")
            {
                Links = downloadLinks
            };
        }

        var componentDownload = new ComponentDownload(
            ReleaseKind.ComponentDownload,
            component,
            version,
            $"{title} Downloads")
        {
            FeatureBand = featureBand,
            Links = HalHelpers.OrderLinks(links),
            Embedded = new ComponentDownloadEmbedded(downloadFiles)
        };

        var json = JsonSerializer.Serialize(
            componentDownload,
            DownloadsIndexSerializerContext.Default.ComponentDownload);

        var schemaUri = $"{Location.GitHubBaseUri}{FileNames.Directories.Schemas}/{FileNames.Schemas.ComponentDownload}";
        var jsonWithSchema = JsonSchemaInjector.JsonSchemaInjector.AddSchemaToContent(json, schemaUri);

        await File.WriteAllTextAsync(filePath, (jsonWithSchema ?? json) + '\n');
    }
}
