using System.Globalization;
using System.Text.Json;
using DotnetRelease;

namespace UpdateIndexes;

public class ReleaseIndexFiles
{

    private readonly List<string> _leafFiles = ["releases.json", "release.json", "manifest.json"];

    // Generates index files for each major version directory and one root index file
    public static async Task GenerateAsync(List<MajorReleaseSummary> summaries, string rootDir)
    {
        if (!Directory.Exists(rootDir))
        {
            throw new DirectoryNotFoundException($"Root directory does not exist: {rootDir}");
        }

        var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
        List<ReleaseIndexEntry> majorEntries = [];

        var summaryTable = summaries.ToDictionary(
            s => s.MajorVersion,
            s => s,
            StringComparer.OrdinalIgnoreCase);

        // Look at all the major version directories
        // The presence of a releases.json file indicates this is a major version directory
        foreach (var majorVersionDir in Directory.EnumerateDirectories(rootDir))
        {
            var majorVersionDirName = Path.GetFileName(majorVersionDir);

            if (!summaryTable.TryGetValue(majorVersionDirName, out var summary))
            {
                continue;
            }

            List<HalTuple> majorVersionTuples = [.. IndexHelpers.GetHalLinksForPath(majorVersionDir, new(majorVersionDir, rootDir), summary.MajorVersionLabel)];
            var majorVersionLinks = majorVersionTuples.ToDictionary(
                t => t.Key,
                t => t.Link);

            // Generate patch version index; release-notes/8.0/index.json
            var patchEntries = GetPatchIndexEntries(summaryTable[majorVersionDirName].PatchReleases, new(majorVersionDir, rootDir));

            var auxFiles = IndexHelpers.GetAuxHalLinksForPath(majorVersionDir, new(majorVersionDir, rootDir), IndexHelpers.AuxFileMappings.Values);

            foreach (var auxFile in auxFiles)
            {
                majorVersionLinks[auxFile.Key] = auxFile.Link;
            }

            var manifestPath = Path.Combine(majorVersionDir, "manifest.json");
            Support? support = null;
            if (File.Exists(manifestPath))
            {
                Console.WriteLine($"Processing manifest file: {manifestPath}");
                Stream manifestStream = File.OpenRead(manifestPath);
                ReleaseManifest manifest = await Hal.GetMajorReleasesIndex(manifestStream) ?? throw new InvalidOperationException($"Failed to read manifest from {manifestPath}");
                support = new Support(manifest.ReleaseType, manifest.SupportPhase, manifest.GaDate, manifest.EolDate);
            }
            else
            {
                support = new Support(summary.ReleaseType, summary.SupportPhase, summary.GaDate, summary.EolDate);
            }

            // write major version index.json if there are patch releases found
            var majorIndexPath = Path.Combine(majorVersionDir, "index.json");
            var relativeMajorIndexPath = Path.GetRelativePath(rootDir, majorIndexPath);
            var patchVersionIndex = new ReleaseIndex(
                majorVersionTuples[0].Kind,
                    $"Index for {summary.MajorVersionLabel} patch releases",
                    majorVersionLinks)
            {
                Embedded = patchEntries.Count > 0 ? new ReleaseIndexEmbedded(patchEntries) : null,
                Support = support
            };

            using Stream patchStream = File.Create(Path.Combine(majorVersionDir, "index.json"));

            JsonSerializer.Serialize(
                patchStream,
                patchVersionIndex,
                ReleaseIndexSerializerContext.Default.ReleaseIndex);

            // Same links as the major version index, but with a different base directory (to force different pathing)
            List<HalTuple> majorVersionWithinAllReleasesIndexTuples = [.. IndexHelpers.GetHalLinksForPath(majorVersionDir, new(majorVersionDir, rootDir), summary.MajorVersionLabel)];
            var majorVersionWithinAllReleasesIndexLinks = majorVersionWithinAllReleasesIndexTuples.ToDictionary(
                t => t.Key,
                t => t.Link);

            // Add the major version entry to the list
            var majorEntry = new ReleaseIndexEntry(
                majorVersionDirName,
                majorVersionWithinAllReleasesIndexTuples[0].Kind,
                majorVersionWithinAllReleasesIndexLinks
                )
            { Support = support };

            majorEntries.Add(majorEntry);
        }

        List<HalTuple> rootHalTuples = [.. IndexHelpers.GetHalLinksForPath(rootDir, new(rootDir), ".NET Release")];
        Console.WriteLine($"Found {rootHalTuples.Count} root links in {rootDir}");
        Dictionary<string, HalLink> rootLinks = rootHalTuples.ToDictionary(
            t => t.Key,
            t => t.Link);

        // Create the major releases index; release-notes/index.json
        var rootIndexPath = Path.Combine(rootDir, "index.json");
        var rootIndexRelativePath = Path.GetRelativePath(rootDir, rootIndexPath);
        var title = "Index of .NET major versions";
        var majorIndex = new ReleaseIndex(
                ReleaseKind.Index,
                title,
                rootLinks)
        {
            Embedded = new ReleaseIndexEmbedded([.. majorEntries.OrderByDescending(e => e.Version, numericStringComparer)])
        };

        // Write the major index file
        using Stream stream = File.Create(Path.Combine(rootDir, "index.json"));
        JsonSerializer.Serialize(
            stream,
            majorIndex,
            ReleaseIndexSerializerContext.Default.ReleaseIndex);
    }

    // Generates index containing each patch release in the major version directory
    private static List<ReleaseIndexEntry> GetPatchIndexEntries(IList<PatchReleaseSummary> summaries, PathContext pathContext)
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

        List<ReleaseIndexEntry> indexEntries = [];

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
            var indexEntry = new ReleaseIndexEntry(summary.PatchVersion, ReleaseKind.PatchRelease, links);
            indexEntries.Add(indexEntry);
        }

        return indexEntries;
    }
}
