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

        cveTable.WriteHeader(cveLabels);

        foreach (Cve cve in cves.Cves)
        {
            cveTable.NewRow()
                    .WriteColumn($"[{cve.Id}][{cve.Id}]")
                    .WriteColumn(cve.Description)
                    .WriteColumn(cve.Product)
                    .WriteColumn(Join(cve.Platforms))
                    .WriteColumn(cve?.Cvss ?? "");
        }
        
        writer.Write(cveTable);
    }

    public static void WritePackageTable(CveSet cves, StreamWriter writer)
    {
        // Package version table
        string[] packageLabels = ["CVE", "Package", "Min Version", "Max Version", "Fixed Version"];
        Table packageTable = new();

        packageTable.WriteHeader(packageLabels);

        foreach (Cve cve in cves.Cves)
        {
            foreach (var package in cve.Packages)
            {
                packageTable.NewRow()
                           .WriteColumn($"[{cve.Id}][{cve.Id}]")
                           .WriteColumn(Report.MakePackageString(package.Name))
                           .WriteColumn($">={package.MinVulnerableVersion}")
                           .WriteColumn($"<={package.MaxVulnerableVersion}")
                           .WriteColumn(package.FixedVersion);
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

        commitTable.WriteHeader(commitLabels);

        foreach (Commit commit in cves.Commits)
        {
            commitTable.NewRow()
                       .WriteColumn($"[{commit.Cve}][{commit.Cve}]")
                       .WriteColumn(Report.MakeLinkFromBestSource(commit, commit.Branch, cves.Source.BranchUrl, null))
                       .WriteColumn(Report.MakeLinkFromBestSource(commit, commit.Hash, cves.Source.CommitUrl, commit.Url));
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
