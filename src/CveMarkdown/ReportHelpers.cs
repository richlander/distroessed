using System.Buffers;
using System.Text;
using DotnetRelease.Security;

namespace ReportHelpers;

public class Report
{
    private static readonly string[] UrlTemplateStrings = ["{org}", "{repo}", "{branch}", "{commit}"];
    private static readonly SearchValues<string> UrlTemplateSearch =
        SearchValues.Create(UrlTemplateStrings, StringComparison.OrdinalIgnoreCase);

    public static string MakeUrlFromSourceScheme(CommitInfo commit, string urlScheme)
    {
        ReadOnlySpan<char> scheme = urlScheme;
        int index = 0;
        StringBuilder link = new();
        ReadOnlySpan<string> replacements = [commit.Org, commit.Repo, commit.Branch, commit.Hash];

        while (scheme.Length > 0 && (index = scheme.IndexOfAny(UrlTemplateSearch)) > -1)
        {
            if (index is -1)
            {
                link.Append(scheme);
                break;
            }
            else if (index > 0)
            {
                link.Append(scheme[..index]);
                scheme = scheme[index..];
            }

            for (int i = 0; i < UrlTemplateStrings.Length; i++)
            {
                if (scheme.StartsWith(UrlTemplateStrings[i]))
                {
                    link.Append(replacements[i]);
                    scheme = scheme[UrlTemplateStrings[i].Length..];
                    break;
                }
            }
        }

        return link.ToString();
    }

    public static string MakeLinkFromBestSource(CommitInfo commit, string? display, string? urlScheme, string? fallbackUrl)
    {
        var url = "";
        if (urlScheme is { })
        {
            url = MakeUrlFromSourceScheme(commit, urlScheme);
        }
        else if (fallbackUrl is { })
        {
            url = fallbackUrl;
        }

        return $"[{display ?? url}]({url})";
    }

    public static string MakeCveLink(Cve cve) => $"https://www.cve.org/CVERecord?id={cve.Id}";

    public static string MakeNuGetLink(string package) => $"https://www.nuget.org/packages/{package}";

    public static string MakeMarkdownSafe(string value) => value.Replace('-', '‑').Replace("*", @"\*");

    public static string MakePackagesString(IEnumerable<Package> packages)
    {
        bool next = false;
        StringBuilder builder = new();

        foreach (Package package in packages)
        {
            if (next)
            {
                builder.Append("<br>");
            }

            builder.Append(MakePackageString(package.Name));
            next = true;
        }

        return builder.ToString();
    }

    public static string MakePackageString(string name)
    {
        if (IsFramework(name))
        {
            return MakeMarkdownSafe(name);
        }
        else
        {
            return $"[{name}][{name}]";
        }
    }

    public static bool IsFramework(string name) => name.StartsWith("Microsoft.") && name.Contains("Core.App.Runtime");
}

