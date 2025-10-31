using System.Text.Json;

namespace UpdateIndexes;

/// <summary>
/// Compares HAL+JSON files using JsonDocument, ignoring the _metadata property
/// which contains timestamp information that always differs between generations.
/// </summary>
public static class HalJsonComparer
{
    /// <summary>
    /// Compares two JSON streams to determine if their content has changed,
    /// excluding the _metadata property from comparison.
    /// </summary>
    /// <param name="existingStream">Stream containing the existing JSON content</param>
    /// <param name="newStream">Stream containing the new JSON content</param>
    /// <returns>True if the content has changed (excluding _metadata), false if they are equivalent</returns>
    public static bool HasContentChanged(Stream existingStream, Stream newStream)
    {
        try
        {
            using var existingDoc = JsonDocument.Parse(existingStream);
            using var newDoc = JsonDocument.Parse(newStream);

            return !JsonElementsEqual(existingDoc.RootElement, newDoc.RootElement, excludeMetadata: true);
        }
        catch
        {
            // If we can't parse either document, consider them different
            return true;
        }
    }

    /// <summary>
    /// Checks if a JSON file needs to be updated by comparing the new content with the existing file.
    /// If the file doesn't exist or the content has changed (excluding _metadata), it should be written.
    /// </summary>
    /// <param name="filePath">Path to the existing file</param>
    /// <param name="newContent">New JSON content to compare against</param>
    /// <returns>True if the file should be written (doesn't exist or content changed), false if no update needed</returns>
    public static bool ShouldWriteFile(string filePath, string newContent)
    {
        if (!File.Exists(filePath))
            return true;

        try
        {
            using var existingStream = File.OpenRead(filePath);
            using var newStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(newContent));
            
            return HasContentChanged(existingStream, newStream);
        }
        catch
        {
            // If we can't read the existing file or parse content, write the new file
            return true;
        }
    }

    /// <summary>
    /// Recursively compares two JsonElements for equality, optionally excluding _metadata properties.
    /// </summary>
    /// <param name="element1">First JsonElement to compare</param>
    /// <param name="element2">Second JsonElement to compare</param>
    /// <param name="excludeMetadata">Whether to exclude _metadata properties from comparison</param>
    /// <returns>True if elements are equal, false otherwise</returns>
    private static bool JsonElementsEqual(JsonElement element1, JsonElement element2, bool excludeMetadata = false)
    {
        if (element1.ValueKind != element2.ValueKind)
            return false;

        switch (element1.ValueKind)
        {
            case JsonValueKind.Object:
                return CompareObjects(element1, element2, excludeMetadata);
            
            case JsonValueKind.Array:
                return CompareArrays(element1, element2, excludeMetadata);
            
            case JsonValueKind.String:
                return element1.GetString() == element2.GetString();
            
            case JsonValueKind.Number:
                return element1.GetRawText() == element2.GetRawText();
            
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                return true; // Value kinds already match
            
            default:
                return element1.GetRawText() == element2.GetRawText();
        }
    }

    /// <summary>
    /// Compares two JSON objects for equality, optionally excluding _metadata properties.
    /// </summary>
    private static bool CompareObjects(JsonElement obj1, JsonElement obj2, bool excludeMetadata)
    {
        var props1 = GetFilteredProperties(obj1, excludeMetadata);
        var props2 = GetFilteredProperties(obj2, excludeMetadata);

        if (props1.Count != props2.Count)
            return false;

        foreach (var prop1 in props1)
        {
            if (!props2.TryGetValue(prop1.Key, out var prop2Value))
                return false;

            if (!JsonElementsEqual(prop1.Value, prop2Value, excludeMetadata))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets properties from a JSON object, optionally filtering out _metadata.
    /// </summary>
    private static Dictionary<string, JsonElement> GetFilteredProperties(JsonElement obj, bool excludeMetadata)
    {
        var properties = new Dictionary<string, JsonElement>();
        
        foreach (var property in obj.EnumerateObject())
        {
            if (excludeMetadata && property.Name == "_metadata")
                continue;
                
            properties[property.Name] = property.Value;
        }
        
        return properties;
    }

    /// <summary>
    /// Compares two JSON arrays for equality.
    /// </summary>
    private static bool CompareArrays(JsonElement array1, JsonElement array2, bool excludeMetadata)
    {
        var length1 = array1.GetArrayLength();
        var length2 = array2.GetArrayLength();

        if (length1 != length2)
            return false;

        var enum1 = array1.EnumerateArray();
        var enum2 = array2.EnumerateArray();

        while (enum1.MoveNext() && enum2.MoveNext())
        {
            if (!JsonElementsEqual(enum1.Current, enum2.Current, excludeMetadata))
                return false;
        }

        return true;
    }
}