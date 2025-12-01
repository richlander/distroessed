using System.Text.Json;
using DotnetRelease;
using DotnetRelease.Graph;

namespace VersionIndex;

public static class ManifestGenerator
{
    public static async Task<ReleaseManifest> GenerateManifestAsync(string majorVersionDir, string version, HalLinkGenerator halLinkGenerator)
    {
        var versionLabel = $".NET {version}";

        // Read partial manifest (_manifest.json is now schema-compatible with manifest.json)
        var partialManifestPath = Path.Combine(majorVersionDir, FileNames.PartialManifest);
        PartialManifest? partial = null;

        if (File.Exists(partialManifestPath))
        {
            try
            {
                var partialJson = await File.ReadAllTextAsync(partialManifestPath);
                partial = JsonSerializer.Deserialize<PartialManifest>(partialJson, ReleaseManifestSerializerContext.Default.PartialManifest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to read {partialManifestPath}: {ex.Message}");
            }
        }

        // Use values from _manifest.json with computed fallbacks
        var releaseType = partial?.ReleaseType ?? (IsEvenMajorVersion(version) ? ReleaseType.LTS : ReleaseType.STS);
        var phase = partial?.Phase ?? SupportPhase.Preview;
        var gaDate = partial?.GaDate;
        var eolDate = partial?.EolDate;

        // Compute effective phase and supported flag
        bool? supported = null;
        if (gaDate.HasValue && eolDate.HasValue)
        {
            phase = ReleaseStability.ComputeEffectivePhase(phase, gaDate.Value);
            var lifecycle = new Lifecycle(releaseType, phase, gaDate.Value, eolDate.Value);
            supported = ReleaseStability.IsSupported(lifecycle);
        }
        else
        {
            Console.WriteLine($"Warning: {version} - Missing ga_date or eol_date in _manifest.json");
        }

        // Generate standard links and merge with partial manifest links
        var links = halLinkGenerator.Generate(
            majorVersionDir,
            ReleaseIndexFiles.MainFileMappings.Values,
            (fileLink, key) => key == HalTerms.Self ? versionLabel : fileLink.Title);

        // Merge in additional links from partial manifest (these override generated links)
        if (partial?.Links != null)
        {
            foreach (var (key, link) in partial.Links)
            {
                // For self link, expand the path to full URL
                if (key == HalTerms.Self && link.Href.StartsWith("/"))
                {
                    links[key] = halLinkGenerator.ExpandLink(link, versionLabel);
                }
                else
                {
                    links[key] = link;
                }
            }
        }

        // Create the final manifest with computed values
        return new ReleaseManifest(
            partial?.Kind ?? ReleaseKind.Manifest,
            partial?.Title ?? $"{versionLabel} Manifest",
            partial?.Version ?? version,
            partial?.Label ?? versionLabel)
        {
            ReleaseType = releaseType,
            Phase = phase,
            Supported = supported,
            GaDate = gaDate,
            EolDate = eolDate,
            Links = HalHelpers.OrderLinks(links),
            Metadata = new GenerationMetadata("1.0", DateTimeOffset.UtcNow, "VersionIndex")
        };
    }

    private static bool IsEvenMajorVersion(string version)
    {
        if (version.Contains('.'))
        {
            var majorPart = version.Split('.')[0];
            if (int.TryParse(majorPart, out int major))
            {
                return major % 2 == 0;
            }
        }
        return false;
    }
}
