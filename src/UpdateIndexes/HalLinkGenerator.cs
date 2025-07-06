using DotnetRelease;

namespace UpdateIndexes;

public class HalLinkGenerator(string rootPath, string urlRootPath)
{
    private readonly string _rootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
    private readonly string _urlRootPath = urlRootPath ?? throw new ArgumentNullException(nameof(urlRootPath));

    public Dictionary<string, HalLink> Generate(string baseDirectory, IEnumerable<FileLink> fileLinks, Func<FileLink, string, string> titleGenerator, Func<string, LinkStyle, string> urlGenerator)
    {
        if (baseDirectory == null)
            throw new ArgumentNullException(nameof(baseDirectory));
        if (fileLinks == null)
            throw new ArgumentNullException(nameof(fileLinks));
        if (titleGenerator == null)
            throw new ArgumentNullException(nameof(titleGenerator));
        if (urlGenerator == null)
            throw new ArgumentNullException(nameof(urlGenerator));

        var result = new Dictionary<string, HalLink>();
        bool isSelf = true;

        foreach (var fileLink in fileLinks)
        {
            var filePath = Path.Combine(baseDirectory, fileLink.File);

            if (!File.Exists(filePath))
            {
                continue;
            }

            string filename = fileLink.File;
            string urlRelativePath = Path.GetRelativePath(_urlRootPath, filePath);
            string relativePath = Path.GetRelativePath(baseDirectory, filePath);
            string name = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
            string extension = Path.GetExtension(filename).ToLowerInvariant();
            bool isMarkdown = ".md".Equals(extension, StringComparison.OrdinalIgnoreCase);
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
                        new HalLink(urlGenerator(urlRelativePath, style))
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