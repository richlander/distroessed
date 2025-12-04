using System.Text.RegularExpressions;

namespace BreakingChangesIndex;

/// <summary>
/// Parses breaking change markdown files from the dotnet/docs repository.
/// </summary>
public partial class BreakingChangeParser
{
    private static readonly Regex YamlFrontmatterRegex = MyYamlFrontmatterRegex();
    private static readonly Regex TitleFromYamlRegex = MyTitleFromYamlRegex();
    private static readonly Regex DescriptionFromYamlRegex = MyDescriptionFromYamlRegex();
    private static readonly Regex DateFromYamlRegex = MyDateFromYamlRegex();
    private static readonly Regex CustomFromYamlRegex = MyCustomFromYamlRegex();
    private static readonly Regex SectionHeaderRegex = MySectionHeaderRegex();
    private static readonly Regex AffectedApiRegex = MyAffectedApiRegex();
    private static readonly Regex XrefRegex = MyXrefRegex();

    /// <summary>
    /// Parses a breaking change markdown file and returns a BreakingChange record.
    /// </summary>
    /// <param name="filePath">Full path to the markdown file</param>
    /// <param name="category">Category name (e.g., "aspnet-core")</param>
    /// <param name="majorVersion">Major version for ID generation (e.g., "10")</param>
    /// <param name="relativePath">Relative path from toc.yml (e.g., "aspnet-core/10/file.md")</param>
    public static BreakingChange? Parse(string filePath, string category, string majorVersion, string relativePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var content = File.ReadAllText(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var relativePathWithoutExtension = relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? relativePath[..^3]
            : relativePath;

        // Extract YAML frontmatter
        var frontmatterMatch = YamlFrontmatterRegex.Match(content);
        if (!frontmatterMatch.Success)
        {
            Console.WriteLine($"Warning: No YAML frontmatter found in {filePath}");
            return null;
        }

        var frontmatter = frontmatterMatch.Groups[1].Value;
        var body = content[(frontmatterMatch.Index + frontmatterMatch.Length)..].Trim();

        // Parse frontmatter fields
        var titleMatch = TitleFromYamlRegex.Match(frontmatter);
        var descriptionMatch = DescriptionFromYamlRegex.Match(frontmatter);
        var dateMatch = DateFromYamlRegex.Match(frontmatter);
        var customMatch = CustomFromYamlRegex.Match(frontmatter);

        var title = titleMatch.Success ? CleanYamlString(titleMatch.Groups[1].Value) : fileName;
        var description = descriptionMatch.Success ? CleanYamlString(descriptionMatch.Groups[1].Value) : null;

        // Generate ID from category and filename
        var id = $"{category}-{majorVersion.Replace(".", "")}-{fileName}";

        // Parse sections from body
        var sections = ParseSections(body);

        // Determine type of breaking change
        var breakingType = DetermineBreakingChangeType(sections);

        // Extract version introduced
        var versionIntroduced = ExtractVersionIntroduced(sections, majorVersion);

        // Extract affected APIs
        var affectedApis = ExtractAffectedApis(sections);

        // Extract recommended action as required_action
        var requiredAction = ExtractSection(sections, "Recommended action");

        // Build references
        var references = new List<BreakingChangeReference>();

        // Documentation URL (uses the relative path from toc.yml)
        var docUrl = $"https://learn.microsoft.com/dotnet/core/compatibility/{relativePathWithoutExtension}";
        references.Add(new BreakingChangeReference("documentation", docUrl) { Title = "Breaking change documentation" });

        // Documentation source URL (uses the relative path from toc.yml)
        var sourceUrl = $"https://raw.githubusercontent.com/dotnet/docs/main/docs/core/compatibility/{relativePath}";
        references.Add(new BreakingChangeReference("documentation-source", sourceUrl) { Title = "Documentation source (markdown)" });

        // GitHub announcement if present in ms.custom
        if (customMatch.Success)
        {
            var customValue = customMatch.Groups[1].Value.Trim();
            if (customValue.StartsWith("http"))
            {
                var announcementTitle = customValue.Contains("github.com") ? "GitHub announcement" : "Related link";
                references.Add(new BreakingChangeReference("announcement", customValue) { Title = announcementTitle });
            }
        }

        // Determine impact level (heuristic based on type)
        var impact = DetermineImpact(breakingType, description ?? "", requiredAction);

        return new BreakingChange(id, title, category)
        {
            Type = breakingType,
            VersionIntroduced = versionIntroduced,
            Description = description,
            Impact = impact,
            RequiredAction = requiredAction,
            References = references.Count > 0 ? references : null,
            AffectedApis = affectedApis?.Count > 0 ? affectedApis : null
        };
    }

    private static string CleanYamlString(string value)
    {
        value = value.Trim();
        // Remove surrounding quotes
        if ((value.StartsWith('"') && value.EndsWith('"')) ||
            (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value[1..^1];
        }
        // Unescape quotes
        value = value.Replace("\\\"", "\"").Replace("\\'", "'");
        // Handle various "Breaking change" prefixes
        var prefixes = new[] { "Breaking change:", "Breaking change -", "Breaking change–", "Breaking change —" };
        foreach (var prefix in prefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = value[prefix.Length..].Trim();
                break;
            }
        }
        return value;
    }

    private static Dictionary<string, string> ParseSections(string body)
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var matches = SectionHeaderRegex.Matches(body);

        for (int i = 0; i < matches.Count; i++)
        {
            var sectionName = matches[i].Groups[1].Value.Trim();
            var startIndex = matches[i].Index + matches[i].Length;
            var endIndex = i + 1 < matches.Count ? matches[i + 1].Index : body.Length;
            var sectionContent = body[startIndex..endIndex].Trim();
            sections[sectionName] = sectionContent;
        }

        return sections;
    }

    private static string DetermineBreakingChangeType(Dictionary<string, string> sections)
    {
        if (sections.TryGetValue("Type of breaking change", out var typeSection))
        {
            var lower = typeSection.ToLowerInvariant();

            // Check for combined binary and source incompatible
            if ((lower.Contains("binary") && lower.Contains("source")) ||
                lower.Contains("binary/source"))
            {
                return "binary-source-incompatible";
            }

            // Check for binary incompatible (before source check since some say "binary incompatible" only)
            if (lower.Contains("binary incompatible") || lower.Contains("binary-incompatible") ||
                lower.Contains("#binary-incompatible"))
            {
                return "binary-incompatible";
            }

            // Check for source incompatible - look for the link anchor or text
            if (lower.Contains("source incompatible") || lower.Contains("source-incompatible") ||
                lower.Contains("#source-incompatible") || lower.Contains("source compatibility") ||
                lower.Contains("#source-compatibility"))
            {
                return "source-incompatible";
            }

            // Check for behavioral change
            if (lower.Contains("behavioral") || lower.Contains("#behavioral-change"))
            {
                return "behavioral-change";
            }
        }

        return "behavioral-change"; // Default
    }

    private static string? ExtractVersionIntroduced(Dictionary<string, string> sections, string majorVersion)
    {
        if (sections.TryGetValue("Version introduced", out var versionSection))
        {
            var trimmed = versionSection.Trim();
            // Handle ".NET 10 Preview 7" -> "10.0-preview.7"
            var match = Regex.Match(trimmed, @"\.NET\s+(\d+)(?:\s+Preview\s+(\d+))?(?:\s+RC\s*(\d+))?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var version = match.Groups[1].Value + ".0";
                if (match.Groups[2].Success)
                {
                    version += $"-preview.{match.Groups[2].Value}";
                }
                else if (match.Groups[3].Success)
                {
                    version += $"-rc.{match.Groups[3].Value}";
                }
                return version;
            }

            // Try to extract just a version number
            var simpleMatch = Regex.Match(trimmed, @"(\d+\.\d+(?:\.\d+)?(?:-[\w.]+)?)");
            if (simpleMatch.Success)
            {
                return simpleMatch.Groups[1].Value;
            }

            // If just ".NET 10" or "10", return the major version
            if (Regex.IsMatch(trimmed, @"^\.?NET\s*\d+$|^\d+$", RegexOptions.IgnoreCase))
            {
                return majorVersion + ".0";
            }
        }

        return majorVersion + ".0";
    }

    private static List<string>? ExtractAffectedApis(Dictionary<string, string> sections)
    {
        if (!sections.TryGetValue("Affected APIs", out var apiSection))
        {
            return null;
        }

        var apis = new List<string>();
        var lines = apiSection.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("N/A", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("None", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Remove list markers
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                trimmed = trimmed[2..].Trim();
            }

            // Extract API name from xref
            var xrefMatch = XrefRegex.Match(trimmed);
            if (xrefMatch.Success)
            {
                apis.Add(xrefMatch.Groups[1].Value);
                continue;
            }

            // Extract API name from markdown comments or backticks
            var apiMatch = AffectedApiRegex.Match(trimmed);
            if (apiMatch.Success)
            {
                apis.Add(apiMatch.Groups[1].Value);
                continue;
            }

            // If it looks like an API (contains dots or angle brackets), add it
            if (trimmed.Contains('.') || trimmed.Contains('<') || trimmed.Contains("::"))
            {
                // Clean up markdown formatting
                trimmed = Regex.Replace(trimmed, @"`([^`]+)`", "$1");
                trimmed = Regex.Replace(trimmed, @"\[([^\]]+)\]\([^)]+\)", "$1");
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    apis.Add(trimmed);
                }
            }
        }

        return apis.Count > 0 ? apis : null;
    }

    private static string? ExtractSection(Dictionary<string, string> sections, string sectionName)
    {
        if (sections.TryGetValue(sectionName, out var content))
        {
            // Clean up the content - remove code blocks for summary
            var cleaned = Regex.Replace(content, @"```[\s\S]*?```", "[code sample]");
            // Truncate if too long
            if (cleaned.Length > 500)
            {
                cleaned = cleaned[..497] + "...";
            }
            return cleaned.Trim();
        }
        return null;
    }

    private static string DetermineImpact(string type, string description, string? requiredAction)
    {
        // High impact: binary incompatible changes
        if (type.Contains("binary"))
        {
            return "high";
        }

        // Check for keywords suggesting high impact
        var combined = (description + " " + (requiredAction ?? "")).ToLowerInvariant();
        if (combined.Contains("must") || combined.Contains("required") ||
            combined.Contains("breaking") || combined.Contains("fail"))
        {
            return "medium";
        }

        // Source incompatible is medium
        if (type == "source-incompatible")
        {
            return "medium";
        }

        // Behavioral changes are typically low impact
        return "low";
    }

    [GeneratedRegex(@"^---\s*\n([\s\S]*?)\n---", RegexOptions.Multiline)]
    private static partial Regex MyYamlFrontmatterRegex();

    [GeneratedRegex(@"^title:\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex MyTitleFromYamlRegex();

    [GeneratedRegex(@"^description:\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex MyDescriptionFromYamlRegex();

    [GeneratedRegex(@"^ms\.date:\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex MyDateFromYamlRegex();

    [GeneratedRegex(@"^ms\.custom:\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex MyCustomFromYamlRegex();

    [GeneratedRegex(@"^##\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex MySectionHeaderRegex();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex MyAffectedApiRegex();

    [GeneratedRegex(@"<xref:([^>?]+)")]
    private static partial Regex MyXrefRegex();
}
