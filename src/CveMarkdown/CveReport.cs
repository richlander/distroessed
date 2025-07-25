using DotnetRelease;
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
        ShouldIncludeSection = (id) =>
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
        "platform-table" => WritePlatformTable,
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
        Table cveTable = new();
        string[] all = ["All"];

        cveTable.AddHeader(cveLabels);

        foreach (CveRecord cve in cves.Records.OrderBy(c => c.Id))
        {
            string link = cve.References is { Count: > 0 } ? cve.References[0] : Report.MakeCveLink(cve);
            _links.Add(cve.Id, link);

            cveTable.AddRow(
                $"[{cve.Id}][{cve.Id}]",
                cve.Title,
                cve?.Severity ?? "",
                cve?.Product ?? "",
                Join(cve?.Platforms ?? all),
                cve?.Cvss ?? ""
            );
        }
        
        writer.Write(cveTable);
    }

    public void WritePlatformTable(CveRecords cves, StreamWriter writer)
    {
        const string none = "No platform components with vulnerabilities reported.";
        var platformPackages = ConvertPlatformToLegacyFormat(cves.Platform);
        WritePackageTableForType(platformPackages, writer, "Component", none);
    }

    public void WritePackageTable(CveRecords cves, StreamWriter writer)
    {
        const string none = "No packages with vulnerabilities reported.";
        var packagesList = ConvertPackagesToLegacyFormat(cves.Packages);

        WritePackageTableForType(packagesList, writer, "Package", none);
    }

    public void WritePackageTableForType(IEnumerable<CvePackage> packages, StreamWriter writer, string type, string noneFound)
    {
        // Package version table
        string[] packageLabels = [type, "Min Version", "Max Version", "Fixed Version", "CVE", "Source fix"];
        Table packageTable = new();
        string[] none = ["Unknown"];

        if (packages.Count() == 0)
        {
            writer.Write(noneFound);
            return;
        }

        packageTable.AddHeader(packageLabels);

        foreach (var package in packages.OrderBy(p => p.Name))
        {
            int count = package.Affected.Count;
            int index = 0;
            string packageString = "";

            if (Report.TryGetPlatformName(package.Name, out var platformName))
            {
                packageString = platformName.Name;
            }
            else
            {
                packageString = $"[{package.Name}][{package.Name}]";
                _links.Add(package.Name, Report.MakeNuGetLink(package.Name));
            }

            foreach (var affected in package.Affected.OrderBy(a => a.Family).ThenBy(a => a.CveId))
            {
                string packageName = index == 0 ? packageString : "";
                index++;

                string fixedString = "";

                if (string.IsNullOrEmpty(affected.Fixed))
                {
                    fixedString = "Unknown";
                }
                else if (Report.IsFramework(package.Name))
                {
                    fixedString = $"[{affected.Fixed}]({Report.MakeReleaseNotesLink(affected.Fixed)})";
                }
                else
                {
                    fixedString = $"[{affected.Fixed}]({Report.MakeNuGetLink(package.Name, affected.Fixed)})";
                }

                string commitString = "";
                if (affected.Commits is not null && affected.Commits.Count > 0)
                {
                    foreach (var commit in affected.Commits)
                    {
                        var abbrevHash = Report.GetAbbreviatedCommitHash(commit);
                        commitString += $"[{abbrevHash}][{abbrevHash}] ";
                    }
                }

                packageTable.AddRow(
                    packageName,
                    $">={affected.MinVulnerable}",
                    $"<={affected.MaxVulnerable}",
                    fixedString,
                    affected.CveId,
                    commitString
                );
            }
        }
        
        writer.Write(packageTable);
    }

    public void WriteCommitTable(CveRecords cves, StreamWriter writer)
    {
        if (cves.Commits is null)
        {
            return;
        }

        // Commits table
        string[] commitLabels = ["Repo", "Branch", "Commit"];
        Table commitTable = new();

        commitTable.AddHeader(commitLabels);

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

            commitTable.AddRow(repoLink, branchLink, commitLink);
        }
        
        writer.Write(commitTable);

        writer.WriteLine();
    }

    public void MakeMarkdownLinks(StreamWriter writer)
    {
        writer.WriteLine();

        foreach (var link in _links)
        {
            writer.WriteLine($"[{link.Key}]: {link.Value}");
        }
    }

    public static async Task<bool> MakeReportForDirectory(string directory, string sourceFilename, string targetFilename, string template)
    {
        if (!Directory.Exists(directory))
        {
            Console.WriteLine($"Directory '{directory}' does not exist.");
            return false;
        }

        int count = 0;

        foreach (string file in Directory.GetFiles(directory, sourceFilename, SearchOption.AllDirectories))
        {
            var success = await MakeReport(file, targetFilename, template);
            if (!success)
            {
                Console.WriteLine($"Failed to generate report for {file}.");
                return false;
            }
            count++;
        }

        if (count == 0)
        {
            Console.WriteLine($"No files matching {sourceFilename} found in {directory}.");
            return false;
        }

        Console.WriteLine($"Generated {count} reports in {directory}.");
        return true;
    }


    public static async Task<bool> MakeReport(string source, string targetFilename, string template)
    {
        if (!File.Exists(template))
        {
            Console.WriteLine($"Template file {template} does not exist.");
            return false;
        }

        if (!File.Exists(source))
        {
            Console.WriteLine($"Source file {source} does not exist.");
            return false;
        }

        Console.WriteLine($"Generating report from {source} ...");
        string directory = Path.GetDirectoryName(source)!;
        string target = Path.Combine(directory, targetFilename);
        using var templateStream = File.OpenRead(template);
        using var templateReader = new StreamReader(templateStream);
        using var targetStream = File.Open(target, FileMode.Create);
        using var targetWriter = new StreamWriter(targetStream);

        using var jsonStream = File.OpenRead(source);
        var cves = await CveSerializer.GetCveRecords(jsonStream);

        if (cves?.Records is null)
        {
            Console.WriteLine("JSON deserialization failed");
            return false;
        }

        CveReport cveReport = new();
        MarkdownTemplate notes = cveReport.CreateTemplate(cves);
        notes.Process(templateReader, targetWriter);
        cveReport.MakeMarkdownLinks(targetWriter);

        Console.WriteLine("Report generated successfully.");
        Console.WriteLine($"Source: {source}");
        Console.WriteLine($"Destination: {target}");

        // Close file
        Close(targetWriter, target);

        return true;

        static void Close(StreamWriter writer, string file)
        {
            writer.Close();
            var writtenFile = File.OpenRead(file);
            long length = writtenFile.Length;
            string path = writtenFile.Name;
            writtenFile.Close();

            Console.WriteLine($"Generated {length} bytes");
            Console.WriteLine(path);
        }
    }

    private static string Join(IEnumerable<string>? strings, string separator = ", ") => strings is null ? "" : string.Join(separator, strings);

    private IEnumerable<CvePackage> ConvertPlatformToLegacyFormat(IReadOnlyDictionary<string, IReadOnlyList<CvePackageAffected>> platform)
    {
        var componentGroups = new Dictionary<string, List<Affected>>();
        
        foreach (var versionGroup in platform)
        {
            foreach (var affected in versionGroup.Value)
            {
                if (!componentGroups.ContainsKey(affected.Component))
                {
                    componentGroups[affected.Component] = new List<Affected>();
                }
                
                componentGroups[affected.Component].Add(new Affected(
                    affected.CveId, 
                    affected.MinVulnerable, 
                    affected.MaxVulnerable, 
                    affected.Fixed)
                {
                    Family = affected.Family,
                    Commits = affected.Commits,
                    Binaries = affected.Binaries
                });
            }
        }
        
        return componentGroups.Select(kvp => new CvePackage(kvp.Key, kvp.Value)).ToList();
    }

    private IEnumerable<CvePackage> ConvertPackagesToLegacyFormat(IReadOnlyDictionary<string, IReadOnlyList<CvePackageAffected>> packages)
    {
        var result = new List<CvePackage>();
        
        foreach (var packageGroup in packages)
        {
            var affected = packageGroup.Value.Select(p => new Affected(
                p.CveId, 
                p.MinVulnerable, 
                p.MaxVulnerable, 
                p.Fixed)
            {
                Family = p.Family,
                Commits = p.Commits,
                Binaries = p.Binaries
            }).ToList();

            result.Add(new CvePackage(packageGroup.Key, affected));
        }
        
        return result;
    }
}
