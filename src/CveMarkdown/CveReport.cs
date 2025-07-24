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
        using Table cveTable = new(Writer.GetWriter(writer));

        cveTable.WriteHeader(cveLabels);

        foreach (Cve cve in cves.Cves)
        {cveTable.WriteColumn($"[{cve.Id}][{cve.Id}]");
            cveTable.WriteColumn(cve.Description);
            cveTable.WriteColumn(cve.Product);
            cveTable.WriteColumn(Join(cve.Platforms));
            cveTable.WriteColumn(cve?.Cvss ?? "");
            cveTable.EndRow();
        }
    }

    public static void WritePackageTable(CveSet cves, StreamWriter writer)
    {
        // Package version table
        string[] packageLabels = ["CVE", "Package", "Min Version", "Max Version", "Fixed Version"];
        using Table packageTable = new(Writer.GetWriter(writer));

        packageTable.WriteHeader(packageLabels);

        foreach (Cve cve in cves.Cves)
        {
            foreach (var package in cve.Packages)
            {
                packageTable.WriteColumn($"[{cve.Id}][{cve.Id}]");
                packageTable.WriteColumn(Report.MakePackageString(package.Name));
                packageTable.WriteColumn($">={package.MinVulnerableVersion}");
                packageTable.WriteColumn($"<={package.MaxVulnerableVersion}");
                packageTable.WriteColumn(package.FixedVersion);
                packageTable.EndRow();
            }
        }
    }

    public static void WriteCommitTable(CveSet cves, StreamWriter writer)
    {
        if (cves.Commits is null)
        {
            return;
        }

        // Commits table
        string[] commitLabels = ["CVE", "Branch", "Commit"];
        using Table commitTable = new(Writer.GetWriter(writer));

        commitTable.WriteHeader(commitLabels);

        foreach (Commit commit in cves.Commits)
        {
            commitTable.WriteColumn($"[{commit.Cve}][{commit.Cve}]");
            commitTable.WriteColumn(Report.MakeLinkFromBestSource(commit, commit.Branch, cves.Source.BranchUrl, null));
            commitTable.WriteColumn(Report.MakeLinkFromBestSource(commit, commit.Hash, cves.Source.CommitUrl, commit.Url));
            commitTable.EndRow();
        }

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
