using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease;

/// <summary>
/// Loads glossary terms from the centralized _glossary.json file.
/// </summary>
public static class GlossaryLoader
{
    public const string GlossaryFileName = "_glossary.json";

    /// <summary>
    /// Loads all glossary terms from the specified directory.
    /// </summary>
    /// <param name="rootDir">The root directory containing _glossary.json</param>
    /// <returns>Dictionary of term keys to definitions</returns>
    public static async Task<Dictionary<string, string>> LoadAsync(string rootDir)
    {
        var glossaryPath = Path.Combine(rootDir, GlossaryFileName);

        if (!File.Exists(glossaryPath))
        {
            throw new FileNotFoundException($"Glossary file not found: {glossaryPath}");
        }

        await using var stream = File.OpenRead(glossaryPath);
        var glossary = await JsonSerializer.DeserializeAsync(stream, GlossarySerializerContext.Default.DictionaryStringString);

        return glossary ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Loads glossary terms, filtering to only the specified keys.
    /// </summary>
    /// <param name="rootDir">The root directory containing _glossary.json</param>
    /// <param name="keys">The term keys to include</param>
    /// <returns>Dictionary of filtered term keys to definitions</returns>
    public static async Task<Dictionary<string, string>> LoadAsync(string rootDir, IEnumerable<string> keys)
    {
        var allTerms = await LoadAsync(rootDir);
        var keySet = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allTerms
            .Where(kvp => keySet.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Loads glossary terms, excluding the specified keys.
    /// </summary>
    /// <param name="rootDir">The root directory containing _glossary.json</param>
    /// <param name="excludeKeys">The term keys to exclude</param>
    /// <returns>Dictionary of term keys to definitions with exclusions applied</returns>
    public static async Task<Dictionary<string, string>> LoadExcludingAsync(string rootDir, IEnumerable<string> excludeKeys)
    {
        var allTerms = await LoadAsync(rootDir);
        var excludeSet = excludeKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allTerms
            .Where(kvp => !excludeSet.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}

[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class GlossarySerializerContext : JsonSerializerContext
{
}
