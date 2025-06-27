using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using DotnetRelease;
using UpdateIndexes;

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

    public static Dictionary<string, HalLink> GetIndexEntriesForFiles(string basePath, string self, IEnumerable<string> files)
    {
        var dict = new Dictionary<string, HalLink>();
        foreach (var file in files)
        {
            var isSelf = file.EndsWith(self);
            var (key, entry) = GetIndexEntriesForFiles(basePath, file, isSelf, !isSelf);
            if (key != null && entry != null)
            {
                dict.Add(key, entry);
            }
        }

        return dict;
    }

    public static (string?, HalLink?) GetIndexEntriesForFiles(string basePath, string file, bool isSelf, bool mustExist)
    {
        var path = Path.Combine(basePath, file);
        if (mustExist && !File.Exists(path))
        {
            return (null, null);
        }

        Dictionary<string, HalLink> links = [];

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
            Title = $"{kind} resource",
            Type = fileType
        };

        var key = isSelf ? HalTerms.Self : filename;

        return (key, entry);
    }

    public static string GetProdPath(string relativePath)
    {
        // Assuming the CDN path is a fixed URL structure
        return $"https://builds.dotnet.microsoft.com/dotnet/release-metadata/{relativePath}";
    }
}
