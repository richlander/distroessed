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
            // Parse as JsonNode to preserve structure
            JsonNode? jsonNode = JsonNode.Parse(jsonContent);
            
            if (jsonNode is not JsonObject jsonObject)
            {
                return null;
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

            // Serialize with indentation
            return newObject.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return null;
        }
    }
}