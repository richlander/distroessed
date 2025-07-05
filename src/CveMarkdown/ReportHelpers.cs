using System.Diagnostics.CodeAnalysis;
using DotnetRelease;

namespace ReportHelpers;

public class Report
{

    public static string MakeRepoUrl(string org, string repo) => $"https://github.com/{org}/{repo}";

    public static string MakeBranchUrl(string org, string repo, string branch) => $"https://github.com/{org}/{repo}/tree/{branch}";

    public static string MakeCommitUrl(string org, string repo, string commit) => $"https://github.com/{org}/{repo}/commit/{commit}";

    public static string MakeCveLink(CveRecord cve) => $"https://www.cve.org/CVERecord?id={cve.Id}";

    public static string MakeNuGetLink(string package, string? version = null)
    {
        if (string.IsNullOrEmpty(version))
        {
            return $"https://www.nuget.org/packages/{package}";
        }
        else
        {
            return $"https://www.nuget.org/packages/{package}/{version}";
        }
    }

    public static string MakeMarkdownSafe(string value) => value.Replace('-', '‑').Replace("*", @"\*");

    public static string GetAbbreviatedCommitHash(string hash)
    {
        if (hash.Length < 7)
            return hash;

        return hash[..7];
    }

    public static IEnumerable<string> GetAbbreviatedCommitHashes(IEnumerable<string> commits)
    {
        foreach (var commit in commits)
        {
            yield return GetAbbreviatedCommitHash(commit);
        }
    }

    public static string MakeReleaseNotesLink(string version)
    {
        // expects a version like "8.0.1"
        string twoPart = version[..3]; // "8.0"
        string url = $"https://github.com/dotnet/core/blob/main/release-notes/{twoPart}/{version}/{version}.md";
        return url;
    }

    public static bool IsFramework(string name) => PlatformComponents.Any(p => p.PackageName == name);

    public static bool TryGetPlatformName(string name, [NotNullWhen(true)] out PlatformName? platformName)
    {
        platformName = PlatformComponents.FirstOrDefault(p => p.PackageName == name);
        if (platformName != null)
        {
            return true;
        }

        platformName = null;
        return false;
    }

    public static List<PlatformName> PlatformComponents { get; } =
    [
        new PlatformName(".NET Runtime", "Microsoft.NETCore.App.Runtime"),
        new PlatformName("ASP.NET Runtime", "Microsoft.AspNetCore.App.Runtime"),
        new PlatformName("Windows Desktop Runtime", "Microsoft.WindowsDesktop.App.Runtime"),
        new PlatformName(".NET SDK", "Microsoft.NET.Sdk")
    ];
}

public record PlatformName(string Name, string PackageName);
