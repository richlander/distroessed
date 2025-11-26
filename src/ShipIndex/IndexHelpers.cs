using System.Net;
using DotnetRelease;
using DotnetRelease.Graph;

namespace ShipIndex;

public class IndexHelpers
{

    private static readonly OrderedDictionary<string, ReleaseKindMapping> _halFileMappings = new()
    {
        { FileNames.Index, new ReleaseKindMapping("index", FileNames.Index, ReleaseKind.Index, MediaType.Json) },
        { FileNames.Releases, new ReleaseKindMapping("releases", FileNames.Releases, ReleaseKind.MajorRelease, MediaType.Json) },
        { FileNames.Release, new ReleaseKindMapping("release", FileNames.Release, ReleaseKind.PatchRelease, MediaType.Json) },
        { FileNames.Manifest, new ReleaseKindMapping("manifest", FileNames.Manifest, ReleaseKind.Manifest, MediaType.Json) },
        { "usage.md", new ReleaseKindMapping("usage", "usage.md", ReleaseKind.Content, MediaType.Markdown) },
        { "terminology.md", new ReleaseKindMapping("terminology", "terminology.md", ReleaseKind.Content, MediaType.Markdown) },
        { $"{FileNames.Directories.Timeline}/{FileNames.Index}", new ReleaseKindMapping("release-timeline", $"{FileNames.Directories.Timeline}/{FileNames.Index}", ReleaseKind.Index, MediaType.HalJson) }
    };

    public static readonly OrderedDictionary<string, FileLink> AuxFileMappings = new()
    {
        {FileNames.SupportedOs, new FileLink(FileNames.SupportedOs, "Supported OSes", LinkStyle.Prod) },
        {"supported-os.md", new FileLink("supported-os.md", "Supported OSes", LinkStyle.Prod | LinkStyle.GitHub) },
        {"linux-packages.json", new FileLink("linux-packages.json", "Linux Packages", LinkStyle.Prod) },
        {"linux-packages.md", new FileLink("linux-packages.md", "Linux Packages", LinkStyle.Prod | LinkStyle.GitHub) },
        {"README.md", new FileLink("README.md", "Release Notes", LinkStyle.GitHub) }
    };

    public static IEnumerable<HalTuple> GetHalLinksForPath(string targetPath, PathContext pathContext, string subtitle)
    {
        var dict = new Dictionary<string, HalLink>();
        bool isSelf = true;

        foreach (ReleaseKindMapping mapping in _halFileMappings.Values)
        {
            var file = Path.Combine(targetPath, mapping.Filename);
            HalTuple? tuple = GetLinkForFile(pathContext, file, isSelf, true, subtitle);
            if (tuple is null)
            {
                continue; // Skip if the file does not exist or is not valid
            }

            isSelf = false; // Only the first entry is self
            yield return tuple;
        }
    }

    public static IEnumerable<HalTuple> GetAuxHalLinksForPath(string targetPath, PathContext pathContext, IEnumerable<FileLink> files)
    {
        foreach (var mapping in files)
        {
            var file = Path.Combine(targetPath, mapping.File);

            if (!File.Exists(file))
            {
                continue;
            }

            string relativePath = Path.GetRelativePath(pathContext.Basepath, file);
            string urlRelativePath = Path.GetRelativePath(pathContext.UrlBasePath ?? pathContext.Basepath, file);
            string pathValue = "/" + relativePath.Replace("\\", "/");
            string filename = mapping.File;
            string name = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
            string extension = Path.GetExtension(filename).ToLowerInvariant();
            bool isMarkdown = ".md".Equals(extension, StringComparison.OrdinalIgnoreCase);

            if (mapping.Style.HasFlag(LinkStyle.Prod))
            {
                // Raw content is the default (no suffix for markdown)
                var key = isMarkdown ? $"{name}-markdown" : name;
                yield return new HalTuple(key, ReleaseKind.Content, new HalLink(GetProdPath(urlRelativePath))
                {
                    Path = pathValue,
                    Title = mapping.Title,
                    Type = extension switch
                    {
                        ".json" => MediaType.Json,
                        ".md" => MediaType.Markdown,
                        _ => MediaType.Text
                    }
                });
            }

            if (mapping.Style.HasFlag(LinkStyle.GitHub))
            {
                // GitHub blob view is the rendered version
                var key = isMarkdown ? $"{name}-markdown-rendered" : name;
                yield return new HalTuple(key, ReleaseKind.Content, new HalLink(GetGitHubPath(urlRelativePath))
                {
                    Path = pathValue,
                    Title = $"{mapping.Title} (Rendered)",
                    Type = MediaType.Markdown
                });
            }
        }
    }

    public static HalTuple? GetLinkForFile(PathContext pathContext, string file, bool isSelf, bool mustExist, string subtitle)
    {
        if (mustExist && !File.Exists(file))
        {
            return null;
        }

        var filename = Path.GetFileNameWithoutExtension(file);
        var relativePath = Path.GetRelativePath(pathContext.Basepath, file);
        var urlRelativePath = Path.GetRelativePath(pathContext.UrlBasePath ?? pathContext.Basepath, file);
        var pathValue = "/" + relativePath.Replace("\\", "/");
        var kind = _halFileMappings.TryGetValue(relativePath, out var mapping) ? mapping.Kind : ReleaseKind.Unknown;
        var type = _halFileMappings.TryGetValue(relativePath, out var fileType) ? fileType.FileType : MediaType.Text;
        var prodPath = GetProdPath(urlRelativePath);

        var link = new HalLink(prodPath)
        {
            Path = pathValue,
            Title = $"{subtitle} {kind}",
            Type = type
        };

        string defaultKey = mapping?.Kind.ToString().ToLowerInvariant() ?? filename.ToLowerInvariant();

        if (defaultKey == "content")
        {
            defaultKey = mapping?.Name.ToLowerInvariant() ?? filename.ToLowerInvariant();
        }

        var key = isSelf ? HalTerms.Self : defaultKey;

        return new HalTuple(key, kind, link);
    }

    public static string GetProdPath(string relativePath) => GetRawGitHubBranchPath(relativePath);

    public static string GetRawGitHubBranchPath(string relativePath) =>
      $"{Location.GitHubBaseUri}{relativePath}";


    public static string GetGitHubPath(string relativePath) =>
    $"https://github.com/dotnet/core/blob/main/release-notes/{relativePath}";
}

public record HalTuple(string Key, ReleaseKind Kind, HalLink Link);
