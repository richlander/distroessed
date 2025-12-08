using DotnetRelease.Graph;

namespace ShipIndex;

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
            string pathValue = "/" + relativePath.Replace("\\", "/");
            string name = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
            string extension = Path.GetExtension(filename).ToLowerInvariant();
            bool isMarkdown = ".md".Equals(extension, StringComparison.OrdinalIgnoreCase);
            
            // Map files to semantic HAL+JSON relations
            if (filename == "timeline/index.json")
            {
                name = LinkRelations.TimelineIndex;
            }
            else if (filename == "usage.md")
            {
                name = "help";
            }
            else if (filename == "glossary.md")
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
                name = LinkRelations.ReleaseManifest;
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
            // Special case for cve.json to use consistent naming
            else if (filename == "cve.json")
            {
                name = LinkRelations.CveJson;
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
                    // Raw content (Prod) is the default, GitHub blob is the rendered version
                    var linkKey = selfKey ?? (isMarkdown ? $"{name}-{(style == LinkStyle.Prod ? "markdown" : "markdown-rendered")}" : name);
                    var baseTitle = titleGenerator(fileLink, linkKey);
                    var title = isMarkdown && style == LinkStyle.GitHub ? $"{baseTitle} (Rendered)" : baseTitle;

                    // GitHub blob view renders markdown as HTML
                    var linkType = style == LinkStyle.GitHub && isMarkdown ? MediaType.Html : fileType;
                    result[linkKey] = new HalLink(urlGenerator(relativePath, style))
                        {
                            Title = title,
                            Type = linkType == MediaType.HalJson ? null : linkType
                        };
                }
            }
        }

        return HalHelpers.OrderLinks(result);
    }
}