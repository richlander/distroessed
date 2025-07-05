using DotnetRelease;

if (args.Length is < 1 || !args[0].EndsWith("cve.json"))
{
    Console.WriteLine("Error: Invalid args.");
    Console.WriteLine("Usage: CveValidate cve.json");
    return;
}

string jsonFilename = args[0];

using var jsonStream = File.OpenRead(jsonFilename);
var cves = await CveSerializer.GetCveRecords(jsonStream);

if (cves?.Records is null)
{
    Console.WriteLine("Error: JSON deserialization failed");
    return;
}

if (cves.Records.Count == 0)
{
    Console.WriteLine("Error: No records found");
    return;
}
Console.WriteLine($"Date: {cves.Date}");
Console.WriteLine($"Cve records: {cves.Records.Count}");
Console.WriteLine($"Packages: {cves.Packages.Count}");
Console.WriteLine($"Commits: {cves.Commits?.Count ?? 0}");
Console.WriteLine();

List<string> all = ["All"];
List<string> none = ["None"];

foreach (var cve in cves.Records.OrderBy(c => c.Id))
{
    if (cve.Id is null)
    {
        Console.WriteLine("Error: CVE is missing ID");
        continue;
    }

    if (cve.Description is null)
    {
        Console.WriteLine($"Error: CVE {cve.Id} is missing description");
        continue;
    }

    Console.WriteLine($"CVE: {cve.Id}");
    Console.WriteLine($"Description: {cve.Description}");
    Console.WriteLine($"CVSS: {cve.Cvss}");
    Console.WriteLine($"Product: {cve.Product}");
    Console.WriteLine($"Platforms: {string.Join(", ", cve.Platforms ?? all)}");
    Console.WriteLine($"References: {string.Join(", ", cve.References ?? none)}");
    Console.WriteLine();
}

Console.WriteLine();

foreach (var package in cves.Packages.OrderBy(p => p.Name))
{
    if (package.Name is null || package.Affected is null)
    {
        Console.WriteLine($"Error: Package {package.Name ?? ""} is missing information");
        continue;
    }

    Console.WriteLine($"Package: {package.Name}");
    foreach (var affected in package.Affected)
    {
        if (affected.CveId is null || affected.MaxVulnerable is null || affected.Fixed is null)
        {
            Console.WriteLine($"Error: Affected {affected.CveId} is missing information");
            continue;
        }

        Console.WriteLine($"CVE: {affected.CveId}");
        Console.WriteLine($"MinVulnerable: {affected.MinVulnerable}");
        Console.WriteLine($"MaxVulnerable: {affected.MaxVulnerable}");
        Console.WriteLine($"Fixed: {affected.Fixed}");
        Console.WriteLine($"MajorVersion: {affected.Family}");
        Console.WriteLine($"Commits: {string.Join("; ", affected.Commits ?? none)}");

        if (cves.Commits is not null && affected.Commits is not null && !affected.Commits.All(c => c.StartsWith("http")))
        {
            var commit = cves.Commits.FirstOrDefault(c => affected.Commits.Contains(c.Hash));
            if (commit == null)
            {
                Console.WriteLine($"Error: Commit {string.Join(", ", affected.Commits)} not found in commits");
            }
        }

        Console.WriteLine();
    }
}

Console.WriteLine();
Console.WriteLine($"Commits: {cves.Commits?.Count ?? 0}");

if (cves.Commits is not null)
{
    foreach (var commit in cves.Commits.OrderBy(c => c.Repo).ThenBy(c => c.Branch).ThenBy(c => c.Hash))
    {
        if (commit.Repo is null || commit.Branch is null || commit.Hash is null)
        {
            Console.WriteLine("Error: Commit is missing information");
            continue;
        }

        Console.WriteLine($"Repo: {commit.Repo}");
        Console.WriteLine($"Branch: {commit.Branch}");
        Console.WriteLine($"Hash: {commit.Hash}");
        Console.WriteLine($"Org: {commit.Org}");
        Console.WriteLine($"Url: {commit.Url}");

        var package = cves.Packages.FirstOrDefault(p => p.Affected.Any(a => a.Commits?.Contains(commit.Hash) ?? false));
        if (package == null)
        {
            Console.WriteLine($"Error: Commit {commit.Hash} not found in packages");
        }

        var commits = cves.Commits.Where(c => c.Hash == commit.Hash);
        if (commits.Count() > 1)
        {
            Console.WriteLine($"Error: Duplicate commit {commit.Hash}");
        }

        Console.WriteLine();
    }
}
