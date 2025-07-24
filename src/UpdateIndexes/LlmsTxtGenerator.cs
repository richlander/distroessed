using System.Text;
using DotnetRelease;
using MarkdownHelpers;

namespace UpdateIndexes;

/// <summary>
/// Generates llms.txt content from HAL+JSON links using a subscription model and markdown templates.
/// The "Getting Started" section subscribes to specific semantic relations,
/// and everything else goes to "Key Data Sources".
/// </summary>
public static class LlmsTxtGenerator
{
    // Relations that "Getting Started" section subscribes to
    private static readonly HashSet<string> GettingStartedSubscriptions = new()
    {
        "self",
        "help", 
        "glossary"
    };

    // Relations to exclude from both sections (internal/technical links)
    private static readonly HashSet<string> ExcludedRelations = new()
    {
        "release-manifest",
        "release-readme"
    };

    public static string Generate(Dictionary<string, HalLink> links, string title = ".NET Release Metadata", string description = "Structured, machine-readable .NET release data designed for AI assistants and automated tooling.")
    {
        // Find the template file relative to the application base directory
        var templatePath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "templates", "llms-template.txt");

        if (!File.Exists(templatePath))
        {
            // Fallback to original string-based generation if template not found
            return GenerateFallback(links, title, description);
        }

        var template = new MarkdownTemplate
        {
            Processor = (key, writer) =>
            {
                switch (key)
                {
                    case "title":
                        writer.Write(title);
                        break;
                    case "description":
                        writer.Write(description);
                        break;
                    case "getting-started-links":
                        WriteLinksSection(writer, links, GettingStartedSubscriptions, true);
                        break;
                    case "key-data-sources-links":
                        WriteLinksSection(writer, links, GettingStartedSubscriptions, false);
                        break;
                    default:
                        writer.Write($"{{{{unknown-token:{key}}}}}");
                        break;
                }
            }
        };

        using var templateReader = new StreamReader(templatePath);
        using var outputStream = new MemoryStream();
        using var streamWriter = new StreamWriter(outputStream);
        
        template.Process(templateReader, streamWriter);
        streamWriter.Flush();
        
        outputStream.Position = 0;
        using var resultReader = new StreamReader(outputStream);
        return resultReader.ReadToEnd();
    }

    private static void WriteLinksSection(StreamWriter writer, Dictionary<string, HalLink> links, HashSet<string> gettingStartedSubscriptions, bool isGettingStarted)
    {
        var filteredLinks = isGettingStarted 
            ? GetSubscribedLinks(links, gettingStartedSubscriptions)
            : GetRemainingLinks(links, gettingStartedSubscriptions);

        foreach (var (relation, link) in filteredLinks.OrderBy(kvp => GetSortOrder(kvp.Key)))
        {
            var linkDescription = GetLinkDescription(relation, link);
            writer.WriteLine($"- [{linkDescription}]({link.Href}): {link.Title ?? linkDescription}");
        }
    }

    private static string GenerateFallback(Dictionary<string, HalLink> links, string title, string description)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine($"# {title}");
        sb.AppendLine($"> {description}");
        sb.AppendLine();

        // Getting Started section
        sb.AppendLine("## Getting Started");
        var gettingStartedLinks = GetSubscribedLinks(links, GettingStartedSubscriptions);
        foreach (var (relation, link) in gettingStartedLinks.OrderBy(kvp => GetSortOrder(kvp.Key)))
        {
            var linkDescription = GetLinkDescription(relation, link);
            sb.AppendLine($"- [{linkDescription}]({link.Href}): {link.Title ?? linkDescription}");
        }
        sb.AppendLine();

        // Key Data Sources section  
        sb.AppendLine("## Key Data Sources");
        var keyDataSourcesLinks = GetRemainingLinks(links, GettingStartedSubscriptions);
        foreach (var (relation, link) in keyDataSourcesLinks.OrderBy(kvp => GetSortOrder(kvp.Key)))
        {
            var linkDescription = GetLinkDescription(relation, link);
            sb.AppendLine($"- [{linkDescription}]({link.Href}): {link.Title ?? linkDescription}");
        }
        sb.AppendLine();

        // Common Queries section (static content)
        sb.AppendLine("## Common Queries");
        sb.AppendLine("- **Version patches**: `/release-notes/{version}/index.json` (e.g., 8.0/index.json)");
        sb.AppendLine("- **Specific release**: `/release-notes/{version}/{patch}/release.json` (e.g., 8.0/8.0.17/release.json)");
        sb.AppendLine("- **CVEs by month**: `/release-notes/archives/{year}/{month}/cve.json` (e.g., archives/2025/06/cve.json)");
        sb.AppendLine("- **OS support**: `/release-notes/{version}/supported-os.json`");
        sb.AppendLine("- **SDK downloads**: Use stable-sdk-downloads template or direct: [8.0 SDK downloads](https://raw.githubusercontent.com/richlander/core/main/release-notes/8.0/sdk/sdk.json)");
        sb.AppendLine();

        // Data Format section (static content)
        sb.AppendLine("## Data Format");
        sb.AppendLine("- **JSON files**: Authoritative structured data (use these primarily)");
        sb.AppendLine("- **Markdown files**: Human-readable fallback content");
        sb.AppendLine("- **HAL navigation**: Follow `_links` for resource discovery and traversal");

        return sb.ToString();
    }

    private static Dictionary<string, HalLink> GetSubscribedLinks(Dictionary<string, HalLink> allLinks, HashSet<string> subscriptions)
    {
        return allLinks
            .Where(kvp => IsSubscribedRelation(kvp.Key, subscriptions) && !IsExcludedRelation(kvp.Key) && !IsGitHubHtmlLink(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static Dictionary<string, HalLink> GetRemainingLinks(Dictionary<string, HalLink> allLinks, HashSet<string> subscriptions)
    {
        return allLinks
            .Where(kvp => !IsSubscribedRelation(kvp.Key, subscriptions) && !IsExcludedRelation(kvp.Key) && !IsGitHubHtmlLink(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static bool IsSubscribedRelation(string relationKey, HashSet<string> subscriptions)
    {
        // Check exact match first
        if (subscriptions.Contains(relationKey))
            return true;
            
        // Check if it's a markdown variant of a subscribed relation (e.g., "help-markdown-raw", "help-markdown")
        foreach (var subscription in subscriptions)
        {
            if (relationKey.StartsWith($"{subscription}-markdown"))
                return true;
        }
        
        return false;
    }

    private static bool IsExcludedRelation(string relationKey)
    {
        // Check exact match first
        if (ExcludedRelations.Contains(relationKey))
            return true;
            
        // Check if it's a markdown variant of an excluded relation
        foreach (var excluded in ExcludedRelations)
        {
            if (relationKey.StartsWith($"{excluded}-markdown"))
                return true;
        }
        
        return false;
    }

    private static bool IsGitHubHtmlLink(string relationKey)
    {
        // Filter out GitHub HTML links (those ending with "-markdown" but not "-markdown-raw")
        return relationKey.EndsWith("-markdown") && !relationKey.EndsWith("-markdown-raw");
    }

    private static string GetLinkDescription(string relation, HalLink link)
    {
        // Extract base relation name for markdown variants
        var baseRelation = GetBaseRelation(relation);
        
        return baseRelation switch
        {
            "self" => "Release index",
            "help" => "Usage guide", 
            "glossary" => "Glossary",
            "archives" => "Security advisories",
            "about" => "Support policy",
            "newest-release" => "Latest release", 
            "lts-release" => "Latest LTS",
            "stable-sdk-downloads" => "Stable SDK downloads",
            _ => relation.Replace("-", " ").ToTitleCase()
        };
    }

    private static int GetSortOrder(string relation)
    {
        // Extract base relation name for markdown variants
        var baseRelation = GetBaseRelation(relation);
        
        return baseRelation switch
        {
            "self" => 1,
            "help" => 2,
            "glossary" => 3,
            "newest-release" => 4,
            "lts-release" => 5,
            "stable-sdk-downloads" => 6,
            "archives" => 7,
            "about" => 8,
            _ => 999
        };
    }

    private static string GetBaseRelation(string relation)
    {
        // Remove markdown suffixes to get base relation name
        if (relation.EndsWith("-markdown-raw"))
            return relation[..^"-markdown-raw".Length];
        if (relation.EndsWith("-markdown"))
            return relation[..^"-markdown".Length];
        return relation;
    }
}

public static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + (words[i].Length > 1 ? words[i][1..].ToLower() : "");
            }
        }
        return string.Join(" ", words);
    }
}