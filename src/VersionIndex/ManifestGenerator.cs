using System.Text.Json;
using DotnetRelease;

namespace VersionIndex;

public static class ManifestGenerator
{
    public static async Task<ReleaseManifest> GenerateManifestAsync(string majorVersionDir, string version, HalLinkGenerator halLinkGenerator)
    {
        var versionNumber = version;
        var versionLabel = $".NET {version}";

        // Read partial manifest if it exists
        var partialManifestPath = Path.Combine(majorVersionDir, "_manifest.json");
        PartialManifest? partialManifest = null;

        if (File.Exists(partialManifestPath))
        {
            try
            {
                var partialJson = await File.ReadAllTextAsync(partialManifestPath);
                partialManifest = JsonSerializer.Deserialize<PartialManifest>(partialJson, ReleaseManifestSerializerContext.Default.PartialManifest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to read {partialManifestPath}: {ex.Message}");
            }
        }

        // Generate computed values
        var computedReleaseType = IsEvenMajorVersion(version) ? ReleaseType.LTS : ReleaseType.STS;
        var computedSupportPhase = SupportPhase.Preview;

        // Use override values or computed defaults
        var releaseType = partialManifest?.ReleaseType ?? computedReleaseType;
        var supportPhase = partialManifest?.SupportPhase ?? computedSupportPhase;

        // Validate lifecycle data
        Lifecycle? lifecycle = null;
        if (partialManifest?.ReleaseDate.HasValue == true && partialManifest?.EolDate.HasValue == true)
        {
            lifecycle = new Lifecycle(releaseType, supportPhase, partialManifest.ReleaseDate.Value, partialManifest.EolDate.Value);
        }
        else
        {
            Console.WriteLine($"Warning: {version} - Lifecycle is null");
        }

        // Generate standard links
        var links = halLinkGenerator.Generate(
            majorVersionDir,
            ReleaseIndexFiles.MainFileMappings.Values,
            (fileLink, key) => key == HalTerms.Self ? versionLabel : fileLink.Title);

        // Merge in additional links from partial manifest
        if (partialManifest?.Links != null)
        {
            foreach (var additionalLink in partialManifest.Links)
            {
                links[additionalLink.Key] = additionalLink.Value;
            }
        }

        // Create the manifest
        var manifest = new ReleaseManifest(
            ReleaseKind.Manifest,
            $"{versionLabel} Manifest",
            links,
            versionNumber,
            versionLabel)
        {
            Lifecycle = lifecycle,
            Metadata = new GenerationMetadata(DateTimeOffset.UtcNow, "UpdateIndexes")
        };

        return manifest;
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
        return false; // Default to false if parsing fails
    }
}
