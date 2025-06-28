using DotnetRelease;

namespace UpdateIndexes;

public class IndexHelpers
{

    public static ReleaseKind GetKindForSource(string source)
    {
        if (source.EndsWith("index.json")) return ReleaseKind.Index;
        if (source.EndsWith("releases.json")) return ReleaseKind.Releases;
        if (source.EndsWith("release.json")) return ReleaseKind.Release;
        if (source.EndsWith("manifest.json")) return ReleaseKind.Manifest;
        return ReleaseKind.Unknown;
    }

    public static Dictionary<string, HalLink> GetIndexEntriesForFiles(string basePath, string self, IEnumerable<string> files, string subtitle)
    {
        var dict = new Dictionary<string, HalLink>();
        foreach (var file in files)
        {
            var isSelf = file.EndsWith(self);
            var (key, entry) = GetIndexEntriesForFile(basePath, file, isSelf, !isSelf, subtitle);
            if (key != null && entry != null)
            {
                dict.Add(key, entry);
            }
        }

        return dict;
    }

    public static (string?, HalLink?) GetIndexEntriesForFile(string basePath, string file, bool isSelf, bool mustExist, string subtitle)
    {
        var path = Path.Combine(basePath, file);
        if (mustExist && !File.Exists(path))
        {
            return (null, null);
        }

        var filename = Path.GetFileNameWithoutExtension(file);
        var relativePath = Path.GetRelativePath(basePath, path);
        var kind = GetKindForSource(file);
        var prodPath = GetProdPath(relativePath);
        var fileType = kind switch
        {
            ReleaseKind.Index => FileTypes.Json,
            ReleaseKind.Manifest => FileTypes.Json,
            ReleaseKind.Releases => FileTypes.Json,
            ReleaseKind.Release => FileTypes.Json,
            _ => FileTypes.Text
        };

        var entry = new HalLink
        {
            Href = prodPath,
            Relative = relativePath,
            Title = $"{subtitle} {kind}",
            Type = fileType
        };

        var key = isSelf ? HalTerms.Self : filename;

        return (key, entry);
    }

    public static ReleaseIndexEntry GetReleaseIndexEntry(string basePath, string version, string subtitle, params IEnumerable<string> files)
    {
        var links = new Dictionary<string, HalLink>();
        var qualifiedPaths = GetQualifiedPaths(basePath, files);
        bool hasSelf = false;

        foreach (var path in qualifiedPaths)
        {
            Console.WriteLine($"Processing path: {path}; hasSelf: {hasSelf}");
            var (key, entry) = GetIndexEntriesForFile(basePath, path, !hasSelf, true, subtitle);
            if (key != null && entry != null)
            {
                hasSelf = true;
                links[key] = entry;
            }
        }

        Console.WriteLine($"Links count: {links.Count}, hasSelf: {hasSelf}");
        var selfEntry = links.FirstOrDefault(x => x.Key == HalTerms.Self).Value ?? throw new InvalidOperationException("Self link not found in index entries.");
        var selfKind = GetKindForSource(selfEntry.Href);

        return new ReleaseIndexEntry
        {
            Version = version,
            Kind = selfKind,
            Links = links
        };
    }

    public static IEnumerable<string> GetQualifiedPaths(string basePath, IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            var path = Path.Combine(basePath, file);
            yield return path;
        }
    }

    public static string GetProdPath(string relativePath)
    {
        // Assuming the CDN path is a fixed URL structure
        return $"https://builds.dotnet.microsoft.com/dotnet/release-metadata/{relativePath}";
    }
}
