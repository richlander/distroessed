using DotnetRelease;

namespace UpdateIndexes;

public class HalHelpers
{
    public static string Self => "self";

    public static Dictionary<string, HalLink> GetHalLinksForPath(string targetPath, PathContext pathContext, bool firstIsSelf, params IEnumerable<FileLink> files)
    {
        var dict = new Dictionary<string, HalLink>();
        var isSelf = !firstIsSelf;
        foreach (var mapping in files)
        {
            var file = Path.Combine(targetPath, mapping.File);

            if (!File.Exists(file))
            {
                continue;
            }

            string filename = mapping.File;
            string urlRelativePath = Path.GetRelativePath(pathContext.UrlBasePath ?? pathContext.Basepath, file);
            string name = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
            string extension = Path.GetExtension(filename).ToLowerInvariant();
            bool isMarkdown = ".md".Equals(extension, StringComparison.OrdinalIgnoreCase);
            var filetype = MediaType.GetFileType(filename);

            string? selfKey = null;

            if (!isSelf)
            {
                selfKey = HalHelpers.Self;
                isSelf = true; // Only the first link is self
            }

            if (mapping.Style.HasFlag(LinkStyle.Prod))
            {
                var title = isMarkdown ? $"{mapping.Title} (Raw Markdown)" : mapping.Title;
                var key = selfKey ?? (isMarkdown ? $"{name}-markdown-raw" : name);
                var link = new HalLink(LinkHelpers.GetProdPath(urlRelativePath))
                {
                    Relative = filename,
                    Title = title,
                    Type = filetype
                };

                dict[key] = link;
            }

            if (mapping.Style.HasFlag(LinkStyle.GitHub))
            {
                var title = isMarkdown ? $"{mapping.Title} (Markdown)" : mapping.Title;
                var key = selfKey ?? (isMarkdown ? $"{name}-markdown" : name);
                var link = new HalLink(LinkHelpers.GetGitHubPath(urlRelativePath))
                {
                    Relative = filename,
                    Title = title,
                    Type = filetype
                };

                dict[key] = link;
            }
        }

        return dict;
    }
}
