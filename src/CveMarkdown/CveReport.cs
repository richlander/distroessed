using CveInfo;
using MarkdownHelpers;
using ReportHelpers;

public static class CveReport
{
    public static MarkdownTemplate CreateTemplate(CveSet cves)
    {
        MarkdownTemplate notes = new()
        {
            Processor = (id, writer) =>
            {
                Action<CveSet, StreamWriter> action = CveReport.SwitchOnId(id);
                action(cves, writer);
            },
            SectionProcessor = (id) =>
            {
                if (id.StartsWith("commit-section"))
                {
                    return cves?.Commits is {};
                }

                return false;
            }
        };

        return notes;
    }

    public static Action<CveSet, StreamWriter> SwitchOnId(string id) => id switch
    {
        "date" => WriteDate,
        "vuln-table" => WriteCveTable,
        "package-table" => WritePackageTable,
        "commit-table" => WriteCommitTable,
        _ => throw new()
    };

    public static void WriteDate(CveSet cves, StreamWriter writer) 
    {
        string date = cves.Date;
        if (DateOnly.TryParse(cves.Date, out DateOnly dateOnly))
        {
            date = dateOnly.ToString("yyyy-MM-dd");
        }

        writer.Write(date);
    }

    public static void WriteCveTable(CveSet cves, StreamWriter writer)
    {
        // CVE table
        string[] cveLabels = ["CVE", "Description", "Product", "Platforms", "CVSS"];
        Table cveTable = new();

        cveTable.AddHeader(cveLabels);

        foreach (Cve cve in cves.Cves)
        {
            cveTable.AddRow(
                $"[{cve.Id}][{cve.Id}]",
                cve.Description,
                cve.Product,
                Join(cve.Platforms),
                cve?.Cvss ?? ""
            );
        }
        
        writer.Write(cveTable);
    }

    public static void WritePackageTable(CveSet cves, StreamWriter writer)
    {
        // Package version table
        string[] packageLabels = ["CVE", "Package", "Min Version", "Max Version", "Fixed Version"];
        Table packageTable = new();

        packageTable.AddHeader(packageLabels);

        foreach (Cve cve in cves.Cves)
        {
            foreach (var package in cve.Packages)
            {
                packageTable.AddRow(
                    $"[{cve.Id}][{cve.Id}]",
                    Report.MakePackageString(package.Name),
                    $">={package.MinVulnerableVersion}",
                    $"<={package.MaxVulnerableVersion}",
                    package.FixedVersion
                );
            }
        }
        
        writer.Write(packageTable);
    }

    public static void WriteCommitTable(CveSet cves, StreamWriter writer)
    {
        if (cves.Commits is null)
        {
            return;
        }

        // Commits table
        string[] commitLabels = ["CVE", "Branch", "Commit"];
        Table commitTable = new();

        commitTable.AddHeader(commitLabels);

        foreach (Commit commit in cves.Commits)
        {
            commitTable.AddRow(
                $"[{commit.Cve}][{commit.Cve}]",
                Report.MakeLinkFromBestSource(commit, commit.Branch, cves.Source.BranchUrl, null),
                Report.MakeLinkFromBestSource(commit, commit.Hash, cves.Source.CommitUrl, commit.Url)
            );
        }
        
        writer.Write(commitTable);

        writer.WriteLine();

        // Write second part of reference-style links
        foreach (var cve in cves.Cves)
        {writer.WriteLine($"[{cve.Id}]: {Report.MakeCveLink(cve)}");
        }

        foreach (var cve in cves.Cves)
        {
            foreach (var package in cve.Packages)
            {
                if (Report.IsFramework(package.Name))
                {
                    continue;
                }

                writer.WriteLine($"[{package.Name}]: {Report.MakeNuGetLink(package.Name)}");
            }
        }
    }

    private static string Join(IEnumerable<string>? strings) => strings is null ? "" : string.Join(", ", strings);
}
