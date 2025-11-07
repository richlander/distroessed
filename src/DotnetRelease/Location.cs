namespace DotnetRelease;

public class Location
{
    public static string OfficialBaseUri { get; private set; } = "https://builds.dotnet.microsoft.com/dotnet/release-metadata/";

    public static string GitHubBaseUri { get; private set; } = "https://raw.githubusercontent.com/richlander/core/main/release-notes/";

    public static string MajorReleasesIndexUri { get; private set; } = "https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json";

    public static string CacheFriendlyNote { get; private set; } = "This file is cache-friendly; follow links for the most current details.";

    public static void SetGitHubCommit(string commitSha)
    {
        GitHubBaseUri = $"https://raw.githubusercontent.com/richlander/core/{commitSha}/release-notes/";
    }
}