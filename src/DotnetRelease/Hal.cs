namespace DotnetRelease;

public record HalLink
{
    public string Href { get; init; } = default!;

    public string? Relative { get; init; }

    public string? Title { get; init; }

    public string? Type { get; init; }
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

public class FileTypes
{
    public const string Markdown = "application/markdown";
    public const string Json = "application/json";
    public const string Text = "text/plain";
    public const string Binary = "application/octet-stream";
    public const string Html = "text/html";
}

public class HalHelpers
{
    public static string GetFileType(ReleaseKind kind) => kind switch
    {
        ReleaseKind.Index => FileTypes.Json,
        ReleaseKind.Manifest => FileTypes.Json,
        ReleaseKind.Releases => FileTypes.Json,
        ReleaseKind.Release => FileTypes.Json,
        _ => FileTypes.Text
    };
}
