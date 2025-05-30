using CveInfo;
using MarkdownHelpers;
using ReportHelpers;

public class CveReport
{
    private readonly Dictionary<string, string> _links = [];

    public MarkdownTemplate CreateTemplate(CveRecords cves) => new()
    {
        Processor = (id, writer) =>
        {
            Action<CveRecords, StreamWriter> action = SwitchOnId(id);
            action(cves, writer);
        },
        SectionProcessor = (id) =>
        {
            if (id.StartsWith("commit-section"))
            {
                return cves?.Commits is { };
            }

            return false;
        }
    };

    public Action<CveRecords, StreamWriter> SwitchOnId(string id) => id switch
    {
        "date" => WriteDate,
        "vuln-table" => WriteCveTable,
        "package-table" => WritePackageTable,
        "commit-table" => WriteCommitTable,
        _ => throw new()
    };

    public void WriteDate(CveRecords cves, StreamWriter writer)
    {
        string date = cves.Date;
        if (DateOnly.TryParse(cves.Date, out DateOnly dateOnly))
        {
            date = dateOnly.ToString("yyyy-MM-dd");
        }

        writer.Write(date);
    }

    public void WriteCveTable(CveRecords cves, StreamWriter writer)
    {
        // CVE table
        string[] cveLabels = ["ID", "Title", "Severity", "Product", "Platforms", "CVSS"];
        int[] cveLengths = [20, 20, 16, 16, 16, 32];
        Table cveTable = new(Writer.GetWriter(writer), cveLengths);
        string[] all = ["All"];

        cveTable.WriteHeader(cveLabels);

        foreach (Cve cve in cves.Records.OrderBy(c => c.Id))
        {
            string link = cve.References is { Count: > 0 } ? cve.References[0] : Report.MakeCveLink(cve);
            _links.Add(cve.Id, link);

            cveTable.WriteColumn($"[{cve.Id}][{cve.Id}]");
            cveTable.WriteColumn(cve.Title);
            cveTable.WriteColumn(cve?.Severity ?? "");
            cveTable.WriteColumn(cve?.Product ?? "");
            cveTable.WriteColumn(Join(cve?.Platforms ?? all));
            cveTable.WriteColumn(cve?.Cvss ?? "");
            cveTable.EndRow();
        }
    }

    public void WritePackageTable(CveRecords cves, StreamWriter writer)
    {
        // Package version table
        string[] packageLabels = ["Package", "Min Version", "Max Version", "Fixed Version", "CVE", "Source fix"];
        int[] packageLengths = [16, 16, 12, 12, 16, 12];
        Table packageTable = new(Writer.GetWriter(writer), packageLengths);
        string[] none = ["Unknown"];

        packageTable.WriteHeader(packageLabels);

        foreach (var package in cves.Packages.OrderBy(p => p.Name))
        {
            int count = package.Affected.Count;
            int index = 0;
            string packageString = "";

            if (Report.IsFramework(package.Name))
            {
                packageString = package.Name;
            }
            else
            {
                packageString = $"[{package.Name}][{package.Name}]";
                _links.Add(package.Name, Report.MakeNuGetLink(package.Name));
            }

            foreach (var affected in package.Affected)
            {
                if (index == 0)
                {
                    packageTable.WriteColumn(packageString);
                    index++;
                }
                else
                {
                    packageTable.WriteColumn("");
                }

                string fixedString = Report.IsFramework(package.Name) ?
                    $"[{affected.Fixed}]({Report.MakeReleaseNotesLink(affected.Fixed)})" :
                    $"[{affected.Fixed}]({Report.MakeNuGetLink(package.Name, affected.Fixed)})";

                packageTable.WriteColumn($">={affected.MinVulnerable}");
                packageTable.WriteColumn($"<={affected.MaxVulnerable}");
                packageTable.WriteColumn(fixedString);
                packageTable.WriteColumn(affected.CveId);
                packageTable.WriteColumn(Join(Report.GetAbberviatedCommitHashes(affected.Commits ?? none), " "));
                packageTable.EndRow();
            }
        }
    }

    public void WriteCommitTable(CveRecords cves, StreamWriter writer)
    {
        if (cves.Commits is null)
        {
            return;
        }

        // Commits table
        string[] commitLabels = ["Repo", "Branch", "Commit"];
        int[] commitLengths = [30, 20, 60];
        Table commitTable = new(Writer.GetWriter(writer), commitLengths);

        commitTable.WriteHeader(commitLabels);

        foreach (Commit commit in cves.Commits.OrderBy(c => c.Org).ThenBy(c => c.Repo).ThenBy(c => c.Branch).ThenBy(c => c.Hash))
        {
            string repoLink = "";
            string branchLink = "";
            string commitLink = "";

            if (!string.IsNullOrEmpty(commit.Org) && !string.IsNullOrEmpty(commit.Repo))
            {
                var branchUrl = Report.MakeBranchUrl(commit.Org, commit.Repo, commit.Branch);
                var commitUrl = Report.MakeCommitUrl(commit.Org, commit.Repo, commit.Hash);
                var abbrevHash = Report.GetAbbreviatedCommitHash(commit.Hash);
                var repoUrl = Report.MakeRepoUrl(commit.Org, commit.Repo);
                var repo = $"{commit.Org}/{commit.Repo}";

                _links.TryAdd(repo, repoUrl);
                _links.TryAdd(commit.Branch, branchUrl);
                _links.TryAdd(abbrevHash, commitUrl);

                repoLink = $"[{repo}][{repo}]";
                branchLink = $"[{commit.Branch}][{commit.Branch}]";
                commitLink = $"[{abbrevHash}][{abbrevHash}]";
            }

            commitTable.WriteColumn(repoLink);
            commitTable.WriteColumn(branchLink);
            commitTable.WriteColumn(commitLink);
            commitTable.EndRow();
        }

        writer.WriteLine();
    }

    public void MakeMarkdownLinks(StreamWriter writer)
    {
        foreach (var link in _links)
        {
            writer.WriteLine($"[{link.Key}]: {link.Value}");
        }
    }

    private static string Join(IEnumerable<string>? strings, string separator = ", ") => strings is null ? "" : string.Join(separator, strings);
}
