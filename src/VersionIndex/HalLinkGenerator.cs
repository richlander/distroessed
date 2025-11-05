using DotnetRelease.Graph;

namespace VersionIndex;

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
            
            // Calculate relative path for URLs
            // If path starts with ../, it's relative to parent of rootPath (repo root)
            string relativePath;
            if (filename.StartsWith("../"))
            {
                // File is outside release-notes (e.g., ../llms/usage.md)
                // Remove the ../ prefix for the URL path
                relativePath = filename.Substring(3); // Remove "../"
            }
            else
            {
                // File is within release-notes, calculate normally
                relativePath = Path.GetRelativePath(_rootPath, filePath);
            }
            
            string name = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
            string extension = Path.GetExtension(filename).ToLowerInvariant();
            bool isMarkdown = ".md".Equals(extension, StringComparison.OrdinalIgnoreCase);
            
            // Map files to semantic HAL+JSON relations
            if (filename == "archives/index.json")
            {
                name = "archives";
            }
            else if (filename == "README.md" || filename.EndsWith("/README.md"))
            {
                name = "usage";
            }
            else if (filename == "quick-ref.md" || filename.EndsWith("/quick-ref.md"))
            {
                name = "quick-reference";
            }
            else if (filename == "glossary.md" || filename.EndsWith("/glossary.md"))
            {
                name = "glossary";
            }
            else if (filename == "support.md")
            {
                name = "about";
            }
            // Special case for manifest.json to use correct key name
            else if (filename == "manifest.json")
            {
                name = "release-manifest";
            }
            // Special case for README.md to use correct key name
            else if (filename == "README.md")
            {
                name = "release-readme";
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