using System.Text.Json;
using CveHandler;
using DotnetRelease.Security;

// CveSynthesize - Synthesize CVE JSON files for historical releases
// 
// This tool creates cve.json files in the timeline structure for releases
// before 2023-11 (when timeline CVE files were first created).
//
// It extracts CVE data from releases.json files and creates properly
// structured cve.json files that can be used by VersionIndex and other tools.
//
// Usage:
//   CveSynthesize <release-notes-path>
//
// Example:
//   CveSynthesize ~/git/core/release-notes

Console.WriteLine("CveSynthesize - Synthesize historical CVE JSON files");
Console.WriteLine();

if (args.Length < 1)
{
    Console.WriteLine("Usage: CveSynthesize <release-notes-path>");
    Console.WriteLine("Example: CveSynthesize ~/git/core/release-notes");
    return 1;
}

var releaseNotesPath = args[0];
if (!Directory.Exists(releaseNotesPath))
{
    Console.WriteLine($"Error: Directory not found: {releaseNotesPath}");
    return 1;
}

Console.WriteLine($"Release notes path: {releaseNotesPath}");
Console.WriteLine();

// Find the earliest existing timeline cve.json to determine cutoff date
var timelinePath = Path.Combine(releaseNotesPath, "timeline");
var cutoffDate = FindEarliestTimelineCveDate(timelinePath);

if (cutoffDate != null)
{
    Console.WriteLine($"Earliest existing timeline CVE file: {cutoffDate:yyyy-MM}");
    Console.WriteLine($"Will synthesize CVE files for releases before this date");
}
else
{
    Console.WriteLine("No existing timeline CVE files found, will process all releases");
}
Console.WriteLine();

// Scan releases.json files for CVE data
var releasesByCveMonth = new Dictionary<string, List<ReleaseWithCves>>();

foreach (var majorVersionDir in Directory.GetDirectories(releaseNotesPath))
{
    var majorVersion = Path.GetFileName(majorVersionDir);
    
    // Skip non-version directories
    if (!majorVersion.Contains('.') || majorVersion == "timeline")
    {
        continue;
    }
    
    var releasesJsonPath = Path.Combine(majorVersionDir, "releases.json");
    if (!File.Exists(releasesJsonPath))
    {
        continue;
    }
    
    Console.WriteLine($"Processing {majorVersion}/releases.json...");
    
    try
    {
        var releasesJson = await File.ReadAllTextAsync(releasesJsonPath);
        var releasesDoc = JsonDocument.Parse(releasesJson);
        
        if (!releasesDoc.RootElement.TryGetProperty("releases", out var releasesArray))
        {
            continue;
        }
        
        foreach (var release in releasesArray.EnumerateArray())
        {
            if (!release.TryGetProperty("release-date", out var releaseDateProp))
            {
                continue;
            }
            
            var releaseDateStr = releaseDateProp.GetString();
            if (string.IsNullOrEmpty(releaseDateStr))
            {
                continue;
            }
            
            if (!DateOnly.TryParse(releaseDateStr, out var releaseDate))
            {
                continue;
            }
            
            // Skip if after cutoff date
            if (cutoffDate != null && releaseDate >= cutoffDate.Value)
            {
                continue;
            }
            
            // Check for CVEs
            if (!release.TryGetProperty("cve-list", out var cveListArray))
            {
                continue;
            }
            
            // Handle null cve-list
            if (cveListArray.ValueKind == JsonValueKind.Null)
            {
                continue;
            }
            
            if (cveListArray.GetArrayLength() == 0)
            {
                continue;
            }
            
            if (!release.TryGetProperty("release-version", out var versionProp))
            {
                continue;
            }
            
            var version = versionProp.GetString();
            if (string.IsNullOrEmpty(version))
            {
                continue;
            }
            
            // Extract CVE IDs
            var cveIds = new List<string>();
            foreach (var cveEntry in cveListArray.EnumerateArray())
            {
                if (cveEntry.TryGetProperty("cve-id", out var cveIdProp))
                {
                    var cveId = cveIdProp.GetString();
                    if (!string.IsNullOrEmpty(cveId))
                    {
                        cveIds.Add(cveId);
                    }
                }
            }
            
            if (cveIds.Count == 0)
            {
                continue;
            }
            
            // Group by year-month
            var yearMonth = $"{releaseDate.Year:D4}-{releaseDate.Month:D2}";
            if (!releasesByCveMonth.ContainsKey(yearMonth))
            {
                releasesByCveMonth[yearMonth] = new List<ReleaseWithCves>();
            }
            
            releasesByCveMonth[yearMonth].Add(new ReleaseWithCves(
                version,
                majorVersion,
                releaseDate,
                cveIds));
            
            Console.WriteLine($"  Found {cveIds.Count} CVE(s) in {version} ({releaseDate:yyyy-MM-dd})");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error processing {releasesJsonPath}: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"Found {releasesByCveMonth.Count} month(s) with CVE data to synthesize");
Console.WriteLine();

// Generate cve.json files for each month
int createdCount = 0;
int skippedCount = 0;
int errorCount = 0;

foreach (var (yearMonth, releases) in releasesByCveMonth.OrderBy(kvp => kvp.Key))
{
    try
    {
        var parts = yearMonth.Split('-');
        var year = parts[0];
        var month = parts[1];
        
        var monthDir = Path.Combine(timelinePath, year, month);
        var cveJsonPath = Path.Combine(monthDir, "cve.json");
        
        // Skip if cve.json already exists
        if (File.Exists(cveJsonPath))
        {
            Console.WriteLine($"Skipping {yearMonth} (cve.json already exists)");
            skippedCount++;
            continue;
        }
        
        Console.WriteLine($"Generating cve.json for {yearMonth}...");
        
        // Create directory if it doesn't exist
        Directory.CreateDirectory(monthDir);
        
        // Fetch MSRC data for this month
        var msrcId = $"{year}-{new DateTime(int.Parse(year), int.Parse(month), 1):MMM}";
        Console.WriteLine($"  Fetching MSRC data for {msrcId}...");
        var msrcData = await MsrcClient.FetchDataAsync(msrcId);
        
        // Build CVE records
        var cveRecords = await BuildCveRecords(yearMonth, releases, msrcData);
        
        // Write to file
        string json = JsonSerializer.Serialize(cveRecords, CveSerializerContext.Default.CveRecords);
        await File.WriteAllTextAsync(cveJsonPath, json);
        
        Console.WriteLine($"  Created {cveJsonPath}");
        Console.WriteLine($"  Contains {cveRecords.Disclosures.Count} CVE(s) from {releases.Count} release(s)");
        createdCount++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error processing {yearMonth}: {ex.Message}");
        errorCount++;
    }
}

Console.WriteLine();
Console.WriteLine($"Summary: Created {createdCount}, Skipped {skippedCount}, Errors {errorCount}");
return errorCount > 0 ? 1 : 0;

static DateOnly? FindEarliestTimelineCveDate(string timelinePath)
{
    if (!Directory.Exists(timelinePath))
    {
        return null;
    }
    
    DateOnly? earliest = null;
    
    foreach (var yearDir in Directory.GetDirectories(timelinePath))
    {
        var year = Path.GetFileName(yearDir);
        if (!int.TryParse(year, out var yearNum))
        {
            continue;
        }
        
        foreach (var monthDir in Directory.GetDirectories(yearDir))
        {
            var month = Path.GetFileName(monthDir);
            if (!int.TryParse(month, out var monthNum))
            {
                continue;
            }
            
            var cveJsonPath = Path.Combine(monthDir, "cve.json");
            if (File.Exists(cveJsonPath))
            {
                var date = new DateOnly(yearNum, monthNum, 1);
                if (earliest == null || date < earliest.Value)
                {
                    earliest = date;
                }
            }
        }
    }
    
    return earliest;
}

static async Task<CveRecords> BuildCveRecords(string yearMonth, List<ReleaseWithCves> releases, Dictionary<string, MsrcCveData>? msrcData)
{
    var parts = yearMonth.Split('-');
    var year = int.Parse(parts[0]);
    var month = int.Parse(parts[1]);
    var monthName = new DateTime(year, month, 1).ToString("MMMM");
    
    // Collect all unique CVE IDs
    var allCveIds = releases.SelectMany(r => r.CveIds).Distinct().OrderBy(id => id).ToList();
    
    // Build disclosures list
    var disclosures = new List<Cve>();
    foreach (var cveId in allCveIds)
    {
        // Get MSRC data if available
        MsrcCveData? msrcCve = null;
        msrcData?.TryGetValue(cveId, out msrcCve);
        
        // Find the earliest release date for this CVE
        var earliestRelease = releases
            .Where(r => r.CveIds.Contains(cveId))
            .OrderBy(r => r.ReleaseDate)
            .First();
        
        // Build CVE record with MSRC data
        var cvss = new Cvss(
            Version: "3.1",
            Vector: msrcCve?.Vector ?? "",
            Score: msrcCve?.Score ?? 0.0m,
            Severity: "",
            Source: "microsoft"
        );
        
        var timeline = new Timeline(
            Disclosure: new Event(earliestRelease.ReleaseDate, "Publicly disclosed"),
            Fixed: new Event(earliestRelease.ReleaseDate, $"Fixed in {earliestRelease.Version}")
        );
        
        Cna? cna = null;
        if (msrcCve is not null && 
            (!string.IsNullOrEmpty(msrcCve.Impact) || 
             !string.IsNullOrEmpty(msrcCve.CnaSeverity) ||
             msrcCve.Acknowledgments is not null ||
             msrcCve.Faqs is not null))
        {
            cna = new Cna(
                Name: "microsoft",
                Severity: msrcCve.CnaSeverity,
                Impact: msrcCve.Impact,
                Acknowledgments: msrcCve.Acknowledgments,
                Faq: msrcCve.Faqs
            );
        }
        
        var cve = new Cve(
            Id: cveId,
            Problem: msrcCve?.Impact ?? "Security Vulnerability",
            Description: new List<string> { $"A security vulnerability exists in .NET. See {cveId} for details." },
            Cvss: cvss,
            Timeline: timeline,
            Platforms: new List<string> { "all" },
            Architectures: new List<string> { "all" },
            References: new List<string> 
            { 
                $"https://msrc.microsoft.com/update-guide/vulnerability/{cveId}",
                $"https://nvd.nist.gov/vuln/detail/{cveId}"
            },
            Weakness: msrcCve?.Weakness,
            Cna: cna
        );
        
        disclosures.Add(cve);
    }
    
    // Build products and packages lists
    var products = new List<Product>();
    var packages = new List<Package>();
    
    foreach (var release in releases)
    {
        foreach (var cveId in release.CveIds)
        {
            // Add a product entry for dotnet-runtime
            products.Add(new Product(
                CveId: cveId,
                Name: "dotnet-runtime",
                MinVulnerable: release.MajorVersion,
                MaxVulnerable: release.Version,
                Fixed: release.Version,
                Release: release.MajorVersion,
                Commits: new List<string>()
            ));
        }
    }
    
    // Generate dictionaries
    var generated = CveDictionaryGenerator.GenerateAll(new CveRecords(
        LastUpdated: "",
        Title: "",
        Disclosures: disclosures,
        Products: products,
        Packages: packages
    ));
    
    return new CveRecords(
        LastUpdated: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        Title: $".NET {monthName} {year}",
        Disclosures: disclosures,
        Products: products,
        Packages: packages,
        Commits: null,
        ProductName: generated.ProductName,
        ProductCves: generated.ProductCves,
        PackageCves: generated.PackageCves,
        ReleaseCves: generated.ReleaseCves,
        SeverityCves: generated.SeverityCves,
        CveReleases: generated.CveReleases,
        CveCommits: null
    );
}

record ReleaseWithCves(
    string Version,
    string MajorVersion,
    DateOnly ReleaseDate,
    List<string> CveIds);
