using CveInfo;
using MarkdownHelpers;
using ReportHelpers;

public static class CveReport
{
    public static MarkdownTemplate CreateTemplate(CveRecords cves)
    {
        MarkdownTemplate notes = new()
        {
            Processor = (id, writer) =>
            {
                Action<CveRecords, StreamWriter> action = CveReport.SwitchOnId(id);
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

        return notes;
    }

    public static Action<CveRecords, StreamWriter> SwitchOnId(string id) => id switch
    {
        "date" => WriteDate,
        "vuln-table" => WriteCveTable,
        "package-table" => WritePackageTable,
        "commit-table" => WriteCommitTable,
        _ => throw new()
    };

    public static void WriteDate(CveRecords set, StreamWriter writer)
    {
        string date = set.LastUpdated;
        if (DateOnly.TryParse(set.LastUpdated, out DateOnly dateOnly))
        {
            date = dateOnly.ToString("yyyy-MM-dd");
        }

        writer.Write(date);
    }

    public static void WriteCveTable(CveRecords cves, StreamWriter writer)
    {
        // CVE table
        string[] cveLabels = ["CVE", "Description", "Platforms", "CVSS"];
        Table cveTable = new();

        cveTable.AddHeader(cveLabels);

        foreach (Cve cve in cves.Cves)
        {
            cveTable.AddRow(
                $"[{cve.Id}][{cve.Id}]",
                Join(cve.Description),
                Join(cve.Platforms),
                cve.Cvss.Vector
            );
        }

        writer.Write(cveTable);
    }

    public static void WritePackageTable(CveRecords cves, StreamWriter writer)
    {
        // Package version table
        string[] packageLabels = ["CVE", "Package", "Min Version", "Max Version", "Fixed Version"];
        Table packageTable = new();

        packageTable.AddHeader(packageLabels);

        foreach (var package in cves.Packages)
        {
            packageTable.AddRow(
                $"[{package.CveId}][{package.CveId}]",
                Report.MakePackageString(package.Name),
                $">={package.MinVulnerable}",
                $"<={package.MaxVulnerable}",
                package.Fixed
            );
        }

        writer.Write(packageTable);
    }

    public static void WriteCommitTable(CveRecords cves, StreamWriter writer)
    {
        if (cves.Commits is null || cves.CveCommits is null)
        {
            return;
        }

        // Commits table
        string[] commitLabels = ["CVE", "Branch", "Commit"];
        Table commitTable = new();

        commitTable.AddHeader(commitLabels);

        foreach (var cveCommitPair in cves.CveCommits)
        {
            string cveId = cveCommitPair.Key;
            foreach (var commitHash in cveCommitPair.Value)
            {
                if (cves.Commits.TryGetValue(commitHash, out var commit))
                {
                    commitTable.AddRow(
                        $"[{cveId}][{cveId}]",
                        $"[{commit.Branch}]({commit.Url.Replace(".diff", "")})",
                        $"[{commit.Hash[..7]}]({commit.Url})"
                    );
                }
            }
        }

        writer.Write(commitTable);

        writer.WriteLine();

        // Write second part of reference-style links
        foreach (var cve in cves.Cves)
        {
            writer.WriteLine($"[{cve.Id}]: {Report.MakeCveLink(cve)}");
        }

        HashSet<string> writtenPackages = new();
        foreach (var package in cves.Packages)
        {
            if (Report.IsFramework(package.Name) || writtenPackages.Contains(package.Name))
            {
                continue;
            }

            writer.WriteLine($"[{package.Name}]: {Report.MakeNuGetLink(package.Name)}");
            writtenPackages.Add(package.Name);
        }
    }

    private static string Join(IEnumerable<string>? strings) => strings is null ? "" : string.Join(", ", strings);
}
