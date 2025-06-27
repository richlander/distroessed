using UpdateIndexes;

public class IndexHelpers
{

    public static ResourceKind GetKindForSource(string source)
    {
        if (source.EndsWith("index.json")) return ResourceKind.Index;
        if (source.EndsWith("releases.json")) return ResourceKind.Releases;
        if (source.EndsWith("release.json")) return ResourceKind.PatchRelease;
        if (source.EndsWith("release-info.json")) return ResourceKind.;
        return ResourceKind.Unknown;
    }

    public static List<Resource> GetResourcesForFiles(string basePath, IEnumerable<string> files)
    {
        List<Resource> list = [];
        foreach (var file in files)
        {
            string path = Path.Combine(basePath, file);
            if (!File.Exists(path)) continue;

            var relativePath = Path.GetRelativePath(basePath, path);
            var kind = GetKindForSource(file);
            list.Add(new Resource(relativePath, kind));
        }

        return list;
    }
}
