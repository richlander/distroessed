using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonSchemaInjector;

public static class JsonSchemaInjector
{
    /// <summary>
    /// Adds a $schema property to the root of a JSON document.
    /// </summary>
    /// <param name="jsonFilePath">Path to the JSON file to modify</param>
    /// <param name="schemaUri">URI of the schema to add</param>
    /// <returns>True if the schema was added successfully, false otherwise</returns>
    public static async Task<bool> AddSchemaAsync(string jsonFilePath, string schemaUri)
    {
        if (!File.Exists(jsonFilePath))
        {
            return false;
        }

        try
        {
            // Read the JSON file
            string jsonContent = await File.ReadAllTextAsync(jsonFilePath);
            
            // Parse as JsonNode to preserve structure
            JsonNode? jsonNode = JsonNode.Parse(jsonContent);
            
            if (jsonNode is not JsonObject jsonObject)
            {
                return false;
            }

            // Remove existing $schema property if it exists
            jsonObject.Remove("$schema");

            // Create a new object with $schema at the beginning
            var newObject = new JsonObject();
            newObject["$schema"] = schemaUri;

            // Copy all other properties
            foreach (var property in jsonObject)
            {
                newObject[property.Key] = property.Value?.DeepClone();
            }

            // Write back to file with indentation
            string updatedJson = newObject.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(jsonFilePath, updatedJson);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Adds a $schema property to the root of a JSON document (synchronous version).
    /// </summary>
    /// <param name="jsonFilePath">Path to the JSON file to modify</param>
    /// <param name="schemaUri">URI of the schema to add</param>
    /// <returns>True if the schema was added successfully, false otherwise</returns>
    public static bool AddSchema(string jsonFilePath, string schemaUri)
    {
        return AddSchemaAsync(jsonFilePath, schemaUri).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Adds a $schema property to JSON content.
    /// </summary>
    /// <param name="jsonContent">JSON content as string</param>
    /// <param name="schemaUri">URI of the schema to add</param>
    /// <returns>Updated JSON content with $schema property, or null if parsing failed</returns>
    public static string? AddSchemaToContent(string jsonContent, string schemaUri)
    {
        try
        {
            // Parse as JsonDocument to preserve property order
            using var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // Use a MemoryStream and Utf8JsonWriter to build the JSON with proper ordering
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            
            writer.WriteStartObject();
            
            // Write $schema first
            writer.WriteString("$schema", schemaUri);
            
            // Copy all other properties in their original order
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name != "$schema") // Skip if it already exists
                {
                    property.WriteTo(writer);
                }
            }
            
            writer.WriteEndObject();
            writer.Flush();
            
            // Convert to string
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            return null;
        }
    }
}