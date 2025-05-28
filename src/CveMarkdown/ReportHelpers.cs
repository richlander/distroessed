using System.Buffers;
using System.Text;
using CveInfo;

namespace ReportHelpers;

public class Report
{

    public static string MakeRepoUrl(string org, string repo) => $"https://github.com/{org}/{repo}";
    public static string MakeBranchUrl(string org, string repo, string branch) => $"https://github.com/{org}/{repo}/tree/{branch}";

    public static string MakeCommitUrl(string org, string repo, string commit) => $"https://github.com/{org}/{repo}/commit/{commit}";

    public static string MakeCveLink(Cve cve) => $"https://www.cve.org/CVERecord?id={cve.Id}";

    public static string MakeNuGetLink(string package) => $"https://www.nuget.org/packages/{package}";

    public static string MakeMarkdownSafe(string value) => value.Replace('-', '‑').Replace("*", @"\*");

    public static string GetAbbreviatedCommitHash(string hash)
    {
        if (hash.Length < 7)
            return hash;

        return hash[..7];
    }

    public static IEnumerable<string> GetAbberviatedCommitHashes(IEnumerable<string> commits)
    {
        foreach (var commit in commits)
        {
            yield return GetAbbreviatedCommitHash(commit);
        }
    }

    public static bool IsFramework(string name) => name.StartsWith("Microsoft.") && name.Contains("Core.App.Runtime");
}

