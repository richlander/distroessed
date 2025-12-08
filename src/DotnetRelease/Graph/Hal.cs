using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease.Graph;

[Description("HAL+JSON hypermedia link providing navigation to related resources")]
public record HalLink(
    [Description("Absolute URL to the linked resource")]
    string Href)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Descriptive title for the linked resource")]
    public string? Title { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("MIME type of the linked resource. Omit for HAL+JSON (default).")]
    public string? Type { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Indicates that the href is a URI template (RFC 6570)")]
    public bool? Templated { get; set; }
}

public class HalTerms
{
    public const string Self = "self";
    public const string Index = "index";
    public const string Releases = "releases";
    public const string Manifest = "manifest";
    public const string ReleaseInfo = "release-info";
    public const string PatchReleasesIndex = "patch-releases-index";
    public const string PatchRelease = "patch-release";
    public const string Next = "next";
    public const string Prev = "prev";
}

public class MediaType
{
    public const string Markdown = "application/markdown";
    public const string Json = "application/json";
    public const string HalJson = "application/hal+json";
    public const string Text = "text/plain";
    public const string Html = "text/html";

    public static List<string> HalJsonFiles { get; } = new()
    {
        "index.json",
        "manifest.json",
        "timeline/index.json",
    };

    public static string GetFileType(string filename)
    {
        string extension = Path.GetExtension(filename).ToLowerInvariant();
        if (extension == ".json")
        {
            if (HalJsonFiles.Contains(filename, StringComparer.OrdinalIgnoreCase))
            {
                return MediaType.HalJson;
            }
            else
            {
                return MediaType.Json;
            }
        }

        return extension switch
        {
            ".md" => MediaType.Markdown,
            _ => MediaType.Text
        };
    }

}

public class HalHelpers
{
    public static string GetFileType(ReleaseKind kind) => kind switch
    {
        ReleaseKind.Index => MediaType.Json,
        ReleaseKind.Manifest => MediaType.Json,
        ReleaseKind.MajorRelease => MediaType.Json,
        ReleaseKind.PatchRelease => MediaType.Json,
        _ => MediaType.Text
    };

    /// <summary>
    /// Orders HAL links with standard relations first (self, next, prev, prev-security), then HAL+JSON links alphabetically,
    /// then non-HAL links (JSON, markdown) alphabetically.
    /// </summary>
    public static Dictionary<string, HalLink> OrderLinks(Dictionary<string, HalLink> links)
    {
        var ordered = new Dictionary<string, HalLink>();
        var standardKeys = new[] { HalTerms.Self, HalTerms.Next, HalTerms.Prev, LinkRelations.PrevSecurity };

        // Add standard relations in preferred order
        if (links.TryGetValue(HalTerms.Self, out var selfLink))
            ordered[HalTerms.Self] = selfLink;
        if (links.TryGetValue(HalTerms.Next, out var nextLink))
            ordered[HalTerms.Next] = nextLink;
        if (links.TryGetValue(HalTerms.Prev, out var prevLink))
            ordered[HalTerms.Prev] = prevLink;
        if (links.TryGetValue(LinkRelations.PrevSecurity, out var prevSecurityLink))
            ordered[LinkRelations.PrevSecurity] = prevSecurityLink;

        // Add HAL+JSON links alphabetically (null type = HAL+JSON default, excluding standard keys already added)
        foreach (var kvp in links.Where(k => (k.Value.Type == MediaType.HalJson || k.Value.Type == null) && !standardKeys.Contains(k.Key)).OrderBy(k => k.Key))
        {
            ordered[kvp.Key] = kvp.Value;
        }

        // Add non-HAL+JSON links alphabetically (JSON, markdown, etc.)
        foreach (var kvp in links.Where(k => k.Value.Type != null && k.Value.Type != MediaType.HalJson).OrderBy(k => k.Key))
        {
            ordered[kvp.Key] = kvp.Value;
        }

        return ordered;
    }
}

public class Hal
{
    public static ValueTask<ReleaseManifest?> GetMajorReleasesIndex(Stream stream) => JsonSerializer.DeserializeAsync<ReleaseManifest>(stream, ReleaseManifestSerializerContext.Default.ReleaseManifest);
}

[Description("Metadata about when and how this document was generated")]
public record GenerationMetadata(
    [Description("Version of the schema used for this document")]
    string SchemaVersion,
    [Description("ISO 8601 timestamp when this document was generated")]
    DateTimeOffset GeneratedOn,
    [Description("Name of the tool or script that generated this document")]
    string GeneratedBy);
