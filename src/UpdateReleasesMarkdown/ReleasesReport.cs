using DotnetRelease;
using MarkdownHelpers;
using System.Text.Json;

namespace UpdateReleasesMarkdown;

public static class ReleasesReport
{
    public static async Task MakeReport(string coreRepoPath, string outputPath, string templatePath)
    {
        // Read the main index.json file
        var mainIndexPath = Path.Combine(coreRepoPath, "index.json");
        if (!File.Exists(mainIndexPath))
        {
            throw new FileNotFoundException($"Main index.json not found at: {mainIndexPath}");
        }

        var mainIndex = await ReadReleaseVersionIndex(mainIndexPath);
        
        // Process releases and separate supported vs EOL
        var supportedReleases = new List<ReleaseInfo>();
        var eolReleases = new List<ReleaseInfo>();
        var releaseLinks = new List<string>();

        foreach (var release in mainIndex.Embedded?.Releases ?? [])
        {
            var releaseInfo = await ProcessRelease(release, coreRepoPath);
            
            if (releaseInfo.Lifecycle?.Supported == true)
            {
                supportedReleases.Add(releaseInfo);
            }
            else
            {
                eolReleases.Add(releaseInfo);
            }

            // Add release link
            if (releaseInfo.LatestPatchVersion != null)
            {
                releaseLinks.Add($"[{releaseInfo.LatestPatchVersion}]: release-notes/{releaseInfo.Version}/{releaseInfo.LatestPatchVersion}/{releaseInfo.LatestPatchVersion}.md");
            }
        }

        // Sort releases: supported by version (descending), EOL by version (descending)
        supportedReleases.Sort((a, b) => string.Compare(b.Version, a.Version, StringComparison.Ordinal));
        eolReleases.Sort((a, b) => string.Compare(b.Version, a.Version, StringComparison.Ordinal));

        // Generate markdown content
        var supportedReleasesMarkdown = GenerateSupportedReleasesTable(supportedReleases);
        var eolReleasesMarkdown = GenerateEolReleasesTable(eolReleases);
        var releaseLinksMarkdown = string.Join("\n", releaseLinks);

        // Process template
        await ProcessTemplate(templatePath, outputPath, supportedReleasesMarkdown, eolReleasesMarkdown, releaseLinksMarkdown);
    }

    private static async Task<ReleaseVersionIndex> ReadReleaseVersionIndex(string indexPath)
    {
        var json = await File.ReadAllTextAsync(indexPath);
        return JsonSerializer.Deserialize(json, ReleaseVersionIndexSerializerContext.Default.ReleaseVersionIndex) 
               ?? throw new InvalidOperationException($"Failed to deserialize index.json: {indexPath}");
    }

    private static async Task<ReleaseInfo> ProcessRelease(ReleaseVersionIndexEntry release, string coreRepoPath)
    {
        // Convert PatchLifecycle to full Lifecycle for compatibility
        Lifecycle? fullLifecycle = null;
        if (release.Lifecycle != null)
        {
            // Infer release type from version (even = LTS, odd = STS)
            var isLts = int.TryParse(release.Version.Split('.')[0], out int majorVersion) && majorVersion % 2 == 0;
            var releaseType = isLts ? ReleaseType.LTS : ReleaseType.STS;
            
            // Calculate EOL date based on release type (LTS = 3 years, STS = 18 months)
            var eolDate = release.Lifecycle.ReleaseDate.AddYears(isLts ? 3 : 0).AddMonths(isLts ? 0 : 18);
            
            fullLifecycle = new Lifecycle(releaseType, release.Lifecycle.Phase, release.Lifecycle.ReleaseDate, eolDate);
            fullLifecycle.Supported = ReleaseStability.IsSupported(fullLifecycle);
        }

        var releaseInfo = new ReleaseInfo
        {
            Version = release.Version,
            Lifecycle = fullLifecycle
        };

        // Try to get latest patch version from version-specific index.json
        var versionIndexPath = Path.Combine(coreRepoPath, release.Version, "index.json");
        if (File.Exists(versionIndexPath))
        {
            try
            {
                var versionIndex = await ReadReleaseVersionIndex(versionIndexPath);
                var latestPatch = versionIndex.Embedded?.Releases?.FirstOrDefault();
                if (latestPatch != null)
                {
                    releaseInfo.LatestPatchVersion = latestPatch.Version;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not read version index for {release.Version}: {ex.Message}");
            }
        }

        return releaseInfo;
    }

    private static string GenerateSupportedReleasesTable(List<ReleaseInfo> releases)
    {
        var rows = new List<string>();
        
        foreach (var release in releases)
        {
            var releaseDate = release.Lifecycle?.ReleaseDate.ToString("MMMM d, yyyy") ?? "TBD";
            var releaseType = release.Lifecycle?.ReleaseType.ToString() ?? "Unknown";
            var phase = release.Lifecycle?.phase.ToString() ?? "Unknown";
            var latestPatch = release.LatestPatchVersion != null ? $"[{release.LatestPatchVersion}][{release.LatestPatchVersion}]" : "TBD";
            var eolDate = release.Lifecycle?.EolDate.ToString("MMMM d, yyyy") ?? "TBD";

            rows.Add($"| [.NET {release.Version}](release-notes/{release.Version}/README.md) | {releaseDate} | [{releaseType}][policies] | {phase} | {latestPatch} | {eolDate} |");
        }

        return string.Join("\n", rows);
    }

    private static string GenerateEolReleasesTable(List<ReleaseInfo> releases)
    {
        var rows = new List<string>();
        
        foreach (var release in releases)
        {
            var releaseDate = release.Lifecycle?.ReleaseDate.ToString("MMMM d, yyyy") ?? "TBD";
            var support = release.Lifecycle?.ReleaseType.ToString() ?? "Unknown";
            var finalPatch = release.LatestPatchVersion != null ? $"[{release.LatestPatchVersion}][{release.LatestPatchVersion}]" : "TBD";
            var eolDate = release.Lifecycle?.EolDate.ToString("MMMM d, yyyy") ?? "TBD";

            rows.Add($"| [.NET {release.Version}](release-notes/{release.Version}/README.md) | {releaseDate} | [{support}][policies] | {finalPatch} | {eolDate} |");
        }

        return string.Join("\n", rows);
    }

    private static Task ProcessTemplate(string templatePath, string outputPath, string supportedReleases, string eolReleases, string releaseLinks)
    {
        var template = new MarkdownTemplate
        {
            Processor = (key, writer) =>
            {
                switch (key)
                {
                    case "SUPPORTED_RELEASES":
                        writer.Write(supportedReleases);
                        break;
                    case "EOL_RELEASES":
                        writer.Write(eolReleases);
                        break;
                    case "RELEASE_LINKS":
                        writer.Write(releaseLinks);
                        break;
                    default:
                        writer.Write($"{{{{{key}}}}}");
                        break;
                }
            }
        };

        using var reader = new StreamReader(templatePath);
        using var writer = new StreamWriter(outputPath);
        template.Process(reader, writer);
        return Task.CompletedTask;
    }

    private class ReleaseInfo
    {
        public string Version { get; set; } = string.Empty;
        public Lifecycle? Lifecycle { get; set; }
        public string? LatestPatchVersion { get; set; }
    }
}