using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[Description("HAL+JSON hyperlink with optional metadata for navigation.")]
public record HalLink(string Href)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Relative { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }
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
}

public class Hal
{
    public static ValueTask<ReleaseManifest?> GetMajorReleasesIndex(Stream stream) => JsonSerializer.DeserializeAsync<ReleaseManifest>(stream, ReleaseManifestSerializerContext.Default.ReleaseManifest);
}
