using DotnetRelease;

namespace UpdateIndexes;

public class HalLinkGenerator(string rootPath, Func<string, LinkStyle, string> urlGenerator)
{
    private readonly string _rootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
    private readonly Func<string, LinkStyle, string> _urlGenerator = urlGenerator ?? throw new ArgumentNullException(nameof(urlGenerator));

    public Dictionary<string, HalLink> Generate(string path,IEnumerable<FileLink> fileLinks, Func<FileLink, string, string> titleGenerator)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(fileLinks);
        ArgumentNullException.ThrowIfNull(titleGenerator);

        var result = new Dictionary<string, HalLink>();
        bool isSelf = true;

        foreach (var fileLink in fileLinks)
        {
            var filePath = Path.Combine(path, fileLink.File);

            if (!File.Exists(filePath))
            {
                continue;
            }

            string filename = fileLink.File;
            string relativePath = Path.GetRelativePath(_rootPath, filePath);
            string name = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
            string extension = Path.GetExtension(filename).ToLowerInvariant();
            bool isMarkdown = ".md".Equals(extension, StringComparison.OrdinalIgnoreCase);
            
            // Special case for history/index.json to use correct key name
            if (filename == "history/index.json")
            {
                name = "release-history-index";
            }
            var fileType = MediaType.GetFileType(filename);

            string? selfKey = null;
            if (isSelf)
            {
                selfKey = HalTerms.Self;
                isSelf = false; // Only the first link is self
            }

            var linkStyles = new[] { LinkStyle.Prod, LinkStyle.GitHub };
            foreach (var style in linkStyles)
            {
                if (fileLink.Style.HasFlag(style))
                {
                    result[selfKey ?? (isMarkdown ? $"{name}-{(style == LinkStyle.Prod ? "markdown-raw" : "markdown")}" : name)] =
                        new HalLink(urlGenerator(relativePath, style))
                        {
                            Relative = relativePath,
                            Title = isMarkdown
                                ? $"{titleGenerator(fileLink, selfKey ?? (isMarkdown ? $"{name}-{(style == LinkStyle.Prod ? "markdown-raw" : "markdown")}" : name))} ({(style == LinkStyle.Prod ? "Raw Markdown" : "Markdown")})"
                                : titleGenerator(fileLink, selfKey ?? (isMarkdown ? $"{name}-{(style == LinkStyle.Prod ? "markdown-raw" : "markdown")}" : name)),
                            Type = fileType
                        };
                }
            }
        }

        return result;
    }
} 