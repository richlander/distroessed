namespace DotnetRelease;

public class Location
{
    public static string OfficialBaseUri { get; private set; } = "https://builds.dotnet.microsoft.com/dotnet/release-metadata/";

    public static string GitHubBaseUri { get; private set; } = "https://raw.githubusercontent.com/richlander/core/main/release-notes/";

    public static string MajorReleasesIndexUri { get; private set; } = "https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json";
}