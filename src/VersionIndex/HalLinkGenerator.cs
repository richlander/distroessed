using DotnetRelease.Graph;

namespace VersionIndex;

public class HalLinkGenerator(string rootPath, Func<string, LinkStyle, string> urlGenerator)
{
    private readonly string _rootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
    private readonly Func<string, LinkStyle, string> _urlGenerator = urlGenerator ?? throw new ArgumentNullException(nameof(urlGenerator));

    public Dictionary<string, HalLink> Generate(string path, IEnumerable<FileLink> fileLinks, Func<FileLink, string, string> titleGenerator, bool includeSelf = true)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(fileLinks);
        ArgumentNullException.ThrowIfNull(titleGenerator);

        var result = new Dictionary<string, HalLink>();
        bool isSelf = includeSelf;

        foreach (var fileLink in fileLinks)
        {
            var filePath = Path.Combine(path, fileLink.File);

            if (!File.Exists(filePath))
            {
                continue;
            }

            string filename = fileLink.File;
            
            // Calculate path relative to release-notes root (starts with /)
            // If path starts with ../, it's relative to parent of rootPath (repo root)
            string pathValue;
            if (filename.StartsWith("../"))
            {
                // File is outside release-notes (e.g., ../llms/usage.md)
                // Remove the ../ prefix and add leading slash
                pathValue = "/" + filename.Substring(3); // Remove "../" and add "/"
            }
            else
            {
                // File is within release-notes, calculate normally and add leading slash
                string relativePath = Path.GetRelativePath(_rootPath, filePath);
                pathValue = "/" + relativePath.Replace("\\", "/");
            }
            
            string name = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
            string extension = Path.GetExtension(filename).ToLowerInvariant();
            bool isMarkdown = ".md".Equals(extension, StringComparison.OrdinalIgnoreCase);
            
            // Map files to semantic HAL+JSON relations
            if (filename == "timeline/index.json")
            {
                name = LinkRelations.TimelineIndex;
            }
            else if (filename == "release-history/index.json")
            {
                name = "release-history";
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
            // Special case for releases.json to match cve-json pattern
            else if (filename == "releases.json")
            {
                name = "releases-json";
            }
            // Special case for release.json to match cve-json pattern
            else if (filename == "release.json")
            {
                name = "release-json";
            }
            // Special case for supported-os.json - non-HAL JSON needs -json suffix
            else if (filename == "supported-os.json")
            {
                name = "supported-os-json";
            }
            // Special case for linux-packages.json - non-HAL JSON needs -json suffix
            else if (filename == "linux-packages.json")
            {
                name = "linux-packages-json";
            }
            // Special case for os-packages.json - non-HAL JSON needs -json suffix
            else if (filename == "os-packages.json")
            {
                name = "os-packages-json";
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
                    // Use pathValue without leading slash for URL generation
                    string urlPath = pathValue.TrimStart('/');
                    // Raw content (Prod) is the default, GitHub blob is the rendered version
                    var linkKey = selfKey ?? (isMarkdown ? $"{name}-{(style == LinkStyle.Prod ? "markdown" : "markdown-rendered")}" : name);
                    var baseTitle = titleGenerator(fileLink, linkKey);
                    var title = isMarkdown && style == LinkStyle.GitHub ? $"{baseTitle} (Rendered)" : baseTitle;

                    result[linkKey] = new HalLink(urlGenerator(urlPath, style))
                        {
                            Title = title,
                            Type = fileType
                        };
                }
            }
        }

        return HalHelpers.OrderLinks(result);
    }

    /// <summary>
    /// Expands a partial link (with relative path) to a full HalLink with absolute URL.
    /// </summary>
    public HalLink ExpandLink(HalLink partialLink, string title)
    {
        // If href is a relative path (starts with /), expand it to full URL
        var href = partialLink.Href;
        if (href.StartsWith("/"))
        {
            href = _urlGenerator(href.TrimStart('/'), LinkStyle.Prod);
        }

        return new HalLink(href)
        {
            Title = partialLink.Title ?? title,
            Type = partialLink.Type ?? MediaType.HalJson
        };
    }
} 