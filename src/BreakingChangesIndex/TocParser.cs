using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BreakingChangesIndex;

/// <summary>
/// Parses the toc.yml file to extract breaking change file paths organized by version and category.
/// </summary>
public static class TocParser
{
    /// <summary>
    /// Parses the toc.yml and returns breaking change file paths for the specified .NET version.
    /// </summary>
    /// <returns>Dictionary mapping category names to lists of file paths</returns>
    public static Dictionary<string, List<string>> GetBreakingChangesByVersion(string tocPath, string version)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(tocPath))
        {
            Console.WriteLine($"Warning: toc.yml not found at {tocPath}");
            return result;
        }

        var yaml = File.ReadAllText(tocPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var toc = deserializer.Deserialize<TocRoot>(yaml);
        if (toc?.Items == null)
        {
            return result;
        }

        // Find "Breaking changes by version" section
        var byVersionSection = toc.Items.FirstOrDefault(i =>
            i.Name?.Contains("by version", StringComparison.OrdinalIgnoreCase) == true);

        if (byVersionSection?.Items == null)
        {
            return result;
        }

        // Find the specific version (e.g., ".NET 10")
        // Handle both "10.0" input and ".NET 10" in toc
        var majorVersion = version.Split('.')[0];
        var versionSection = byVersionSection.Items.FirstOrDefault(i =>
            i.Name?.Equals($".NET {majorVersion}", StringComparison.OrdinalIgnoreCase) == true ||
            i.Name?.Equals($".NET {version}", StringComparison.OrdinalIgnoreCase) == true ||
            i.Name?.Contains(version, StringComparison.OrdinalIgnoreCase) == true);

        if (versionSection?.Items == null)
        {
            Console.WriteLine($"Warning: No breaking changes found for version {version}");
            return result;
        }

        // Process each category under this version
        foreach (var categoryItem in versionSection.Items)
        {
            if (categoryItem.Name == null)
            {
                continue;
            }

            // Skip "Overview" entries
            if (categoryItem.Name.Equals("Overview", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Skip external links (like EF Core)
            if (categoryItem.Href?.StartsWith("/") == true || categoryItem.Href?.StartsWith("http") == true)
            {
                continue;
            }

            var categoryName = NormalizeCategoryName(categoryItem.Name);
            var files = new List<string>();

            if (categoryItem.Items != null)
            {
                foreach (var breakingChangeItem in categoryItem.Items)
                {
                    if (!string.IsNullOrEmpty(breakingChangeItem.Href) &&
                        breakingChangeItem.Href.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    {
                        files.Add(breakingChangeItem.Href);
                    }
                }
            }

            if (files.Count > 0)
            {
                result[categoryName] = files;
            }
        }

        return result;
    }

    private static string NormalizeCategoryName(string name)
    {
        // Convert display names to folder-style names
        return name.ToLowerInvariant() switch
        {
            "asp.net core" => "aspnet-core",
            "core .net libraries" => "core-libraries",
            "sdk and msbuild" => "sdk",
            "windows presentation foundation (wpf)" => "wpf",
            "entity framework core" => "efcore",
            _ => name.ToLowerInvariant().Replace(" ", "-").Replace(".", "")
        };
    }
}

public class TocRoot
{
    public List<TocItem>? Items { get; set; }
}
