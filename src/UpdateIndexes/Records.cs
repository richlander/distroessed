using System.Text.Json;
using System.Text.Json.Serialization;

namespace UpdateIndexes;

public record Resources(ResourceEntry Self, List<ResourceEntry> Entries);

public record ResourceEntry(
    string Key,
    string Value,
    ResourceKind Kind)
{
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Resource>? Resources { get; set; }
}

// Custom naming policy for lower case enum serialization
public class LowerCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) => name.ToLowerInvariant();
}

// NOTE: To serialize ResourceKind as lower-case strings, configure the converter globally:
// options.Converters.Add(new JsonStringEnumConverter(new LowerCaseNamingPolicy()));
public enum ResourceKind
{
    Unknown,
    Index,
    PatchReleasesIndex,
    Releases,
    PatchRelease,
}

public record Resource(string Value, ResourceKind Kind)
{
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
};
