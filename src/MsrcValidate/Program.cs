using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DotnetRelease.Cves;

if (args.Length < 2)
{
    Console.WriteLine("Usage: MsrcValidate <validate|update> <cve-json-path> [<cve-json-path> ...]");
    Console.WriteLine("  validate: Read-only validation of CVE data against MSRC");
    Console.WriteLine("  update: Update CVE data with correct MSRC information");
    return 1;
}

string mode = args[0].ToLower();
if (mode != "validate" && mode != "update")
{
    Console.WriteLine($"Invalid mode: {mode}. Use 'validate' or 'update'.");
    return 1;
}

bool updateMode = mode == "update";
var cveFilePaths = args.Skip(1).ToArray();

int totalIssues = 0;
int filesProcessed = 0;

foreach (var cveFilePath in cveFilePaths)
{
    if (!File.Exists(cveFilePath))
    {
        Console.WriteLine($"File not found: {cveFilePath}");
        continue;
    }

    filesProcessed++;
    Console.WriteLine($"\nProcessing: {cveFilePath}");
    
    var issues = await ProcessCveFile(cveFilePath, updateMode);
    totalIssues += issues;
}

Console.WriteLine($"\n{(updateMode ? "Updated" : "Validated")} {filesProcessed} file(s), found {totalIssues} issue(s).");
return totalIssues > 0 ? 1 : 0;

static async Task<int> ProcessCveFile(string cveFilePath, bool updateMode)
{
    var json = await File.ReadAllTextAsync(cveFilePath);
    var options = new JsonSerializerOptions 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    var cveRecords = JsonSerializer.Deserialize<CveRecords>(json, options);
    
    if (cveRecords == null)
    {
        Console.WriteLine("  Failed to deserialize CVE records.");
        return 0;
    }

    // Extract year and month from file path (assumes format: .../YYYY/MM/cve.json)
    var match = Regex.Match(cveFilePath, @"(\d{4})/(\d{2})/cve\.json");
    if (!match.Success)
    {
        Console.WriteLine("  Could not extract year/month from path. Expected format: .../YYYY/MM/cve.json");
        return 0;
    }

    string year = match.Groups[1].Value;
    string month = match.Groups[2].Value;
    string monthName = new DateTime(int.Parse(year), int.Parse(month), 1).ToString("MMM");
    string msrcId = $"{year}-{monthName}";
    
    Console.WriteLine($"  Fetching MSRC data for {msrcId}...");
    var msrcData = await FetchMsrcData(msrcId);
    
    if (msrcData == null)
    {
        Console.WriteLine("  Failed to fetch MSRC data.");
        return 0;
    }

    int issueCount = 0;
    bool modified = false;

    foreach (var cve in cveRecords.Disclosures)
    {
        if (msrcData.TryGetValue(cve.Id, out var msrcCve))
        {
            var cveIssues = ValidateCve(cve, msrcCve, updateMode, out var updatedCve);
            issueCount += cveIssues.Count;
            
            if (updateMode && updatedCve != null)
            {
                // Replace CVE in list
                var index = cveRecords.Disclosures.ToList().IndexOf(cve);
                if (index >= 0)
                {
                    var cveList = cveRecords.Disclosures.ToList();
                    cveList[index] = updatedCve;
                    cveRecords = cveRecords with { Disclosures = cveList };
                    modified = true;
                }
            }

            foreach (var issue in cveIssues)
            {
                Console.WriteLine($"  [{cve.Id}] {issue}");
            }
        }
    }

    if (updateMode && modified)
    {
        var serializeOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        var updatedJson = JsonSerializer.Serialize(cveRecords, serializeOptions);
        await File.WriteAllTextAsync(cveFilePath, updatedJson);
        Console.WriteLine($"  Updated {cveFilePath}");
    }

    return issueCount;
}

static List<string> ValidateCve(Cve cve, MsrcCveData msrcCve, bool updateMode, out Cve? updatedCve)
{
    updatedCve = null;
    var issues = new List<string>();
    bool needsUpdate = false;
    var newCvss = cve.Cvss;
    var newCna = cve.Cna;
    var newWeakness = cve.Weakness;

    if (cve.Cvss.Score != msrcCve.Score)
    {
        issues.Add($"Score mismatch: {cve.Cvss.Score} (current) vs {msrcCve.Score} (MSRC)");
        newCvss = newCvss with { Score = msrcCve.Score };
        needsUpdate = true;
    }

    if (cve.Cvss.Vector != msrcCve.Vector)
    {
        issues.Add($"Vector mismatch: {cve.Cvss.Vector} (current) vs {msrcCve.Vector} (MSRC)");
        newCvss = newCvss with { Vector = msrcCve.Vector };
        needsUpdate = true;
    }

    // Check CVSS severity based on score
    string expectedSeverity = msrcCve.Score switch
    {
        >= 9.0m => "critical",
        >= 7.0m => "high",
        >= 4.0m => "medium",
        _ => "low"
    };

    if (cve.Cvss.Severity != expectedSeverity)
    {
        issues.Add($"CVSS Severity mismatch: {cve.Cvss.Severity} (current) vs {expectedSeverity} (expected for score {msrcCve.Score})");
        newCvss = newCvss with { Severity = expectedSeverity };
        needsUpdate = true;
    }

    if (msrcCve.Weakness != null && cve.Weakness != msrcCve.Weakness)
    {
        issues.Add($"Weakness mismatch: {cve.Weakness ?? "(null)"} (current) vs {msrcCve.Weakness} (MSRC)");
        newWeakness = msrcCve.Weakness;
        needsUpdate = true;
    }

    // CNA validation
    if (cve.Cna != null)
    {
        if (cve.Cna.Impact != msrcCve.Impact)
        {
            issues.Add($"Impact mismatch: {cve.Cna.Impact} (current) vs {msrcCve.Impact} (MSRC)");
            newCna = newCna! with { Impact = msrcCve.Impact };
            needsUpdate = true;
        }

        if (msrcCve.CnaSeverity != null && cve.Cna.Severity != msrcCve.CnaSeverity)
        {
            issues.Add($"CNA Severity mismatch: {cve.Cna.Severity ?? "(null)"} (current) vs {msrcCve.CnaSeverity} (MSRC)");
            newCna = newCna! with { Severity = msrcCve.CnaSeverity };
            needsUpdate = true;
        }
    }
    else
    {
        // CNA is null, create it with MSRC data
        newCna = new Cna("microsoft", msrcCve.CnaSeverity, msrcCve.Impact);
        needsUpdate = true;
        issues.Add($"CNA missing - will add Microsoft CNA info");
    }

    if (updateMode && needsUpdate)
    {
        updatedCve = cve with 
        { 
            Cvss = newCvss,
            Weakness = newWeakness,
            Cna = newCna
        };
    }

    return issues;
}

static async Task<Dictionary<string, MsrcCveData>?> FetchMsrcData(string msrcId)
{
    using var httpClient = new HttpClient();
    var url = $"https://api.msrc.microsoft.com/cvrf/v2.0/cvrf/{msrcId}";
    
    try
    {
        var xmlContent = await httpClient.GetStringAsync(url);
        return ParseMsrcXml(xmlContent);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error fetching MSRC data: {ex.Message}");
        return null;
    }
}

static Dictionary<string, MsrcCveData> ParseMsrcXml(string xmlContent)
{
    var result = new Dictionary<string, MsrcCveData>();
    
    // Parse embedded HTML table from DocumentNotes (it's HTML-escaped in XML)
    var tableMatch = Regex.Match(xmlContent, @"&lt;table&gt;.*?&lt;/table&gt;", RegexOptions.Singleline);
    if (!tableMatch.Success)
        return result;

    var tableHtml = tableMatch.Value
        .Replace("&lt;", "<")
        .Replace("&gt;", ">")
        .Replace("&amp;", "&");

    // Extract rows
    var rowMatches = Regex.Matches(tableHtml, @"<tr>(.*?)</tr>", RegexOptions.Singleline);
    
    foreach (Match rowMatch in rowMatches)
    {
        var row = rowMatch.Groups[1].Value;
        var cells = Regex.Matches(row, @"<td>(.*?)</td>", RegexOptions.Singleline)
            .Select(m => Regex.Replace(m.Groups[1].Value, @"<a[^>]*>(.*?)</a>", "$1").Trim())
            .ToList();

        if (cells.Count >= 4 && cells[1].StartsWith("CVE-"))
        {
            var cveId = cells[1];
            var scoreText = cells[2];
            var vector = cells[3];

            if (decimal.TryParse(scoreText, out decimal score))
            {
                result[cveId] = new MsrcCveData
                {
                    CveId = cveId,
                    Score = score,
                    Vector = vector,
                    Impact = "", // Will be filled from XML
                    Weakness = null // Will be filled from XML
                };
            }
        }
    }

    // Parse XML for Impact, Weakness (CWE), and MSRC Severity
    var xdoc = XDocument.Parse(xmlContent);
    XNamespace vulnNs = "http://www.icasi.org/CVRF/schema/vuln/1.1";

    foreach (var vuln in xdoc.Descendants(vulnNs + "Vulnerability"))
    {
        var cveElem = vuln.Element(vulnNs + "CVE");
        if (cveElem == null) continue;

        var cveId = cveElem.Value;
        if (!result.ContainsKey(cveId)) continue;

        // Get Impact (first one found)
        var impactElem = vuln.Descendants(vulnNs + "Threat")
            .FirstOrDefault(t => t.Attribute("Type")?.Value == "Impact");
        if (impactElem != null)
        {
            var impactDesc = impactElem.Element(vulnNs + "Description")?.Value ?? "";
            result[cveId] = result[cveId] with { Impact = impactDesc };
        }

        // Get CNA Severity (first one found)
        var severityElem = vuln.Descendants(vulnNs + "Threat")
            .FirstOrDefault(t => t.Attribute("Type")?.Value == "Severity");
        if (severityElem != null)
        {
            var severityDesc = severityElem.Element(vulnNs + "Description")?.Value ?? "";
            result[cveId] = result[cveId] with { CnaSeverity = severityDesc };
        }

        // Get CWE
        var cweElem = vuln.Element(vulnNs + "CWE");
        if (cweElem != null)
        {
            var cweId = cweElem.Attribute("ID")?.Value;
            if (cweId != null)
            {
                result[cveId] = result[cveId] with { Weakness = cweId };
            }
        }
    }

    return result;
}

record MsrcCveData
{
    required public string CveId { get; init; }
    required public decimal Score { get; init; }
    required public string Vector { get; init; }
    required public string Impact { get; init; }
    public string? Weakness { get; init; }
    public string? CnaSeverity { get; init; }
}
