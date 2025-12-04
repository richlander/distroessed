using System.Text.Json;
using System.Text.Json.Serialization;

namespace BreakingChangesIndex;

/// <summary>
/// Generates breaking-changes.json files from the dotnet/docs compatibility documentation.
/// </summary>
public static class BreakingChangesGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Generates a breaking-changes.json file for the specified .NET version.
    /// </summary>
    /// <param name="docsCompatibilityPath">Path to docs/core/compatibility in the dotnet/docs repo</param>
    /// <param name="outputPath">Path to write the breaking-changes.json file</param>
    /// <param name="version">The .NET major version (e.g., "10.0", "9.0")</param>
    /// <param name="schemaUri">Optional schema URI to include in the output</param>
    public static async Task GenerateAsync(
        string docsCompatibilityPath,
        string outputPath,
        string version,
        string? schemaUri = null)
    {
        Console.WriteLine($"Generating breaking changes for .NET {version}");

        // Parse toc.yml to get the list of breaking change files by category
        var tocPath = Path.Combine(docsCompatibilityPath, "toc.yml");
        var breakingChangesByCategory = TocParser.GetBreakingChangesByVersion(tocPath, version);

        if (breakingChangesByCategory.Count == 0)
        {
            Console.WriteLine($"No breaking changes found for .NET {version}");
            return;
        }

        var allBreakingChanges = new List<BreakingChange>();
        var categoriesIndex = new Dictionary<string, List<string>>();

        // Extract the version number for folder matching (e.g., "10" from "10.0")
        var versionFolder = GetVersionFolder(version);

        foreach (var (category, files) in breakingChangesByCategory)
        {
            Console.WriteLine($"  Processing {category}: {files.Count} files");
            var categoryBreakingChanges = new List<string>();

            foreach (var relativePath in files)
            {
                var filePath = Path.Combine(docsCompatibilityPath, relativePath);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"    Warning: File not found: {filePath}");
                    continue;
                }

                var breakingChange = BreakingChangeParser.Parse(filePath, category, versionFolder, relativePath);
                if (breakingChange != null)
                {
                    allBreakingChanges.Add(breakingChange);
                    categoryBreakingChanges.Add(breakingChange.Id);
                }
            }

            if (categoryBreakingChanges.Count > 0)
            {
                categoriesIndex[category] = categoryBreakingChanges;
            }
        }

        Console.WriteLine($"  Total breaking changes: {allBreakingChanges.Count}");

        // Sort breaking changes by category then by ID
        allBreakingChanges = [.. allBreakingChanges.OrderBy(b => b.Category).ThenBy(b => b.Id)];

        // Calculate breakdowns
        var typeBreakdown = allBreakingChanges
            .Where(b => b.Type != null)
            .GroupBy(b => b.Type!)
            .ToDictionary(g => g.Key, g => g.Count());

        var impactBreakdown = allBreakingChanges
            .Where(b => b.Impact != null)
            .GroupBy(b => b.Impact!)
            .ToDictionary(g => g.Key, g => g.Count());

        // Create the document
        var document = new BreakingChangesDocument(
            schemaUri,
            version,
            $".NET {version} Breaking Changes",
            $"Breaking changes introduced in .NET {version} across all technology areas",
            allBreakingChanges.Count)
        {
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Breaks = allBreakingChanges,
            Categories = categoriesIndex.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value),
            TypeBreakdown = typeBreakdown,
            ImpactBreakdown = impactBreakdown,
            Metadata = new BreakingChangesMetadata(
                "1.0",
                DateTimeOffset.UtcNow,
                "BreakingChangesIndex")
            {
                SourceRepository = "https://github.com/dotnet/docs",
                SourcePath = $"docs/core/compatibility/{versionFolder}.md"
            }
        };

        // Write output
        var json = JsonSerializer.Serialize(document, JsonOptions);
        await File.WriteAllTextAsync(outputPath, json + "\n");

        Console.WriteLine($"  Written to: {outputPath}");
    }

    private static string GetVersionFolder(string version)
    {
        // Convert "10.0" to "10" for folder matching
        // But handle cases like "10.0" -> "10.0" if that's the actual folder
        var parts = version.Split('.');
        return parts[0];
    }
}
