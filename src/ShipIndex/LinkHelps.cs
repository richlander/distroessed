using DotnetRelease;

namespace ShipIndex;

public class LinkHelpers
{
    public static string GetProdPath(string relativePath) => GetRawGitHubBranchPath(relativePath);

    public static string GetCdnPath(string relativePath) =>
      $"https://builds.dotnet.microsoft.com/dotnet/release-metadata/{relativePath}";


    public static string GetRawGitHubPath(string relativePath) =>
      $"https://raw.githubusercontent.com/dotnet/core/main/release-notes/{relativePath}";

    public static string GetRawGitHubBranchPath(string relativePath) =>
      $"{Location.GitHubBaseUri}{relativePath}";


    public static string GetGitHubPath(string relativePath) =>
    $"https://github.com/dotnet/core/blob/main/release-notes/{relativePath}";
}
