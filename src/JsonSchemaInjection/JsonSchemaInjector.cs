using System.Text.Json;

namespace JsonSchemaInjection;

/// <summary>
/// Provides functionality to inject JSON schema references into JSON documents.
/// </summary>
public static class JsonSchemaInjector
{
    /// <summary>
    /// Adds a "$schema" property to the root of a JSON document.
    /// </summary>
    /// <param name="jsonFilePath">Path to the JSON file to modify</param>
    /// <param name="schemaUrl">URL of the JSON schema to reference</param>
    /// <returns>True if the schema was successfully added, false otherwise</returns>
    public static bool AddSchemaToFile(string jsonFilePath, string schemaUrl)
    {
        try
        {
            if (!File.Exists(jsonFilePath))
            {
                return false;
            }

            var jsonContent = File.ReadAllText(jsonFilePath);
            var modifiedJson = AddSchemaToJson(jsonContent, schemaUrl);
            
            if (modifiedJson != null)
            {
                File.WriteAllText(jsonFilePath, modifiedJson);
                return true;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Adds a "$schema" property to the root of a JSON string.
    /// </summary>
    /// <param name="jsonContent">JSON content as a string</param>
    /// <param name="schemaUrl">URL of the JSON schema to reference</param>
    /// <returns>Modified JSON string with schema reference, or null if failed</returns>
    public static string? AddSchemaToJson(string jsonContent, string schemaUrl)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // Create a new JSON object with $schema as the first property
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            
            writer.WriteStartObject();
            
            // Write the $schema property first
            writer.WriteString("$schema", schemaUrl);
            
            // Copy all existing properties (except $schema if it already exists)
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name != "$schema")
                {
                    property.WriteTo(writer);
                }
            }
            
            writer.WriteEndObject();
            writer.Flush();
            
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Batch adds schema references to multiple JSON files.
    /// </summary>
    /// <param name="filesToSchemaMap">Dictionary mapping file paths to their schema URLs</param>
    /// <returns>Dictionary of file paths to success status</returns>
    public static Dictionary<string, bool> AddSchemaToFiles(Dictionary<string, string> filesToSchemaMap)
    {
        var results = new Dictionary<string, bool>();
        
        foreach (var (filePath, schemaUrl) in filesToSchemaMap)
        {
            results[filePath] = AddSchemaToFile(filePath, schemaUrl);
        }
        
        return results;
    }

    /// <summary>
    /// Determines the appropriate schema URL based on the kind property in the JSON.
    /// </summary>
    /// <param name="jsonContent">JSON content as a string</param>
    /// <param name="baseSchemaUrl">Base URL for schema files</param>
    /// <returns>The appropriate schema URL, or null if kind cannot be determined</returns>
    public static string? GetSchemaUrlFromKind(string jsonContent, string baseSchemaUrl)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("kind", out var kindElement) && kindElement.ValueKind == JsonValueKind.String)
            {
                var kind = kindElement.GetString();
                return kind switch
                {
                    "index" => $"{baseSchemaUrl}/release-version-index.json",
                    "release-history-index" => $"{baseSchemaUrl}/release-history-index.json",
                    "release-history-year-index" => $"{baseSchemaUrl}/history-year-index.json",
                    "release-history-month-index" => $"{baseSchemaUrl}/history-month-index.json",
                    "manifest" => $"{baseSchemaUrl}/release-manifest.json",
                    _ => null
                };
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}