using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DotnetRelease;
using DotnetRelease.Security;

// CveValidate - Validate and update CVE JSON files
// Usage:
//   CveValidate validate <path> [--skip-urls]    - Validate cve.json file(s)
//   CveValidate update <path> [--skip-urls]      - Update dictionaries and CVSS scores in cve.json file(s)
//
// Examples:
//   CveValidate validate ~/git/core/release-notes/archives
//   CveValidate validate ~/git/core/release-notes/archives/2024/01/cve.json --skip-urls
//   CveValidate update ~/git/core/release-notes/archives/2024/01/cve.json
//
// Update command fetches CVSS scores from CVE.org API and updates:
//   - Query dictionaries (cve_releases, product_cves, package_cves, etc.)
//   - CVSS scores and severity ratings from authoritative CVE.org data
//   - CWE/weakness information from CVE.org
//   - CNA severity, impact, acknowledgments, and FAQs from MSRC (unless --skip-urls is used)
//   - Cve_commits dictionary

const string jsonFilename = "cve.json";

Console.WriteLine("CveValidate");

if (args.Length < 1)
{
    ReportInvalidArgs();
    return 1;
}

// Parse arguments
string? command = null;
string? inputPath = null;
bool skipUrls = false;
bool quietMode = false;

foreach (var arg in args)
{
    if (arg == "--skip-urls")
    {
        skipUrls = true;
    }
    else if (arg == "--quiet" || arg == "-q")
    {
        quietMode = true;
    }
    else if (!arg.StartsWith("--"))
    {
        if (command is null)
        {
            command = arg.ToLowerInvariant();
        }
        else
        {
            inputPath = arg;
        }
    }
}

// If only one non-option argument provided, treat as path with validate command
if (command is not null && inputPath is null)
{
    inputPath = command;
    command = "validate";
}

if (inputPath is null)
{
    ReportInvalidArgs();
    return 1;
}

if (command != "validate" && command != "update")
{
    Console.WriteLine($"Error: Invalid command '{command}'. Must be 'validate' or 'update'.");
    ReportInvalidArgs();
    return 1;
}

// Determine if input is a file or directory
List<string> cveFiles = new();

if (File.Exists(inputPath))
{
    if (!inputPath.EndsWith(jsonFilename))
    {
        Console.WriteLine($"Error: Input file must be named '{jsonFilename}'");
        return 1;
    }
    cveFiles.Add(inputPath);
}
else if (Directory.Exists(inputPath))
{
    cveFiles.AddRange(Directory.GetFiles(inputPath, jsonFilename, SearchOption.AllDirectories));
    
    if (cveFiles.Count == 0)
    {
        Console.WriteLine($"No '{jsonFilename}' files found in directory: {inputPath}");
        return 1;
    }
    
    // Sort files alphabetically for consistent chronological order
    cveFiles.Sort();
}
else
{
    Console.WriteLine($"Error: Path not found: {inputPath}");
    return 1;
}

Console.WriteLine($"Found {cveFiles.Count} CVE file(s) to process");
if (!quietMode)
{
    Console.WriteLine();
}

int successCount = 0;
int failureCount = 0;

foreach (var cveFile in cveFiles)
{
    bool success = command == "validate" 
        ? await ValidateCveFile(cveFile, skipUrls, quietMode) 
        : await UpdateCveFile(cveFile, skipUrls);
    
    if (success)
    {
        successCount++;
    }
    else
    {
        failureCount++;
    }
    
    if (!quietMode || !success)
    {
        Console.WriteLine();
    }
}

string action = command == "validate" ? "Validation" : "Update";
Console.WriteLine($"{action} complete: {successCount} succeeded, {failureCount} failed");
return failureCount > 0 ? 1 : 0;

static async Task<bool> ValidateCveFile(string filePath, bool skipUrls, bool quietMode)
{
    var errors = new List<string>();

    try
    {
        // Load and deserialize the JSON
        using var jsonStream = File.OpenRead(filePath);
        var cves = await CveUtils.GetCves(jsonStream);

        if (cves is null)
        {
            errors.Add("Failed to deserialize JSON");
            Console.WriteLine($"Validating: {filePath}");
            ReportErrors(errors);
            return false;
        }

        // Run all validations
        ValidateTaxonomy(cves, errors);
        ValidateProblemDescriptionMatch(cves, errors);
        ValidateVersionCoherence(cves, errors);
        ValidateReleaseFields(cves, errors);
        ValidateReleaseVersionFormats(cves, errors);
        ValidateCommitBranchMatch(cves, errors);
        ValidateForeignKeys(cves, errors);
        ValidateDictionaries(cves, errors);
        await ValidateNuGetPackages(cves, errors);
        await ValidateAgainstReleasesJson(filePath, cves, errors);

        if (!skipUrls)
        {
            await ValidateUrls(cves, errors);
            await ValidateMsrcData(filePath, cves, errors);
        }

        if (errors.Count == 0)
        {
            if (!quietMode)
            {
                Console.WriteLine($"Validating: {filePath}");
                Console.WriteLine("  ✓ All validations passed");
            }
            return true;
        }
        else
        {
            Console.WriteLine($"Validating: {filePath}");
            ReportErrors(errors);
            return false;
        }
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Validating: {filePath}");
        errors.Add($"JSON parsing error: {ex.Message}");
        ReportErrors(errors);
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Validating: {filePath}");
        errors.Add($"Unexpected error: {ex.Message}");
        ReportErrors(errors);
        return false;
    }
}

static async Task<IList<Cve>> UpdateCvssScores(IList<Cve> cves)
{
    var updatedCves = new List<Cve>();
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("User-Agent", "dotnet-cve-tools");
    
    foreach (var cve in cves)
    {
        try
        {
            Console.WriteLine($"  Fetching CVE data for {cve.Id}...");
            var response = await client.GetStringAsync($"https://cveawg.mitre.org/api/cve/{cve.Id}");
            var jsonDoc = JsonDocument.Parse(response);
            
            decimal baseScore = 0.0m;
            string baseSeverity = "";
            string? weakness = null;
            
            // Navigate to containers.cna
            if (jsonDoc.RootElement.TryGetProperty("containers", out var containers) &&
                containers.TryGetProperty("cna", out var cna))
            {
                // Get CVSS metrics
                if (cna.TryGetProperty("metrics", out var metrics) &&
                    metrics.GetArrayLength() > 0)
                {
                    var firstMetric = metrics[0];
                    if (firstMetric.TryGetProperty("cvssV3_1", out var cvssV3_1))
                    {
                        baseScore = cvssV3_1.TryGetProperty("baseScore", out var scoreElement) 
                            ? scoreElement.GetDecimal() 
                            : 0.0m;
                        baseSeverity = cvssV3_1.TryGetProperty("baseSeverity", out var severityElement)
                            ? severityElement.GetString() ?? ""
                            : "";
                    }
                }
                
                // Get CWE from problemTypes
                if (cna.TryGetProperty("problemTypes", out var problemTypes) &&
                    problemTypes.GetArrayLength() > 0)
                {
                    var firstProblem = problemTypes[0];
                    if (firstProblem.TryGetProperty("descriptions", out var descriptions) &&
                        descriptions.GetArrayLength() > 0)
                    {
                        var firstDesc = descriptions[0];
                        if (firstDesc.TryGetProperty("cweId", out var cweIdElement))
                        {
                            weakness = cweIdElement.GetString();
                        }
                    }
                }
            }
            
            // Update CVSS with score and severity
            var updatedCvss = cve.Cvss with
            {
                Score = baseScore,
                Severity = baseSeverity
            };
            
            updatedCves.Add(cve with { Cvss = updatedCvss, Weakness = weakness });
            Console.WriteLine($"    Score: {baseScore}, Severity: {baseSeverity}, CWE: {weakness ?? "none"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    Error fetching CVE data: {ex.Message}");
            updatedCves.Add(cve);
        }
        
        // Rate limiting - be nice to the API
        await Task.Delay(500);
    }
    
    return updatedCves;
}

static async Task<IList<Cve>> UpdateCnaDataFromMsrc(string filePath, IList<Cve> cves)
{
    var msrcData = await FetchMsrcDataForFile(filePath);
    if (msrcData is null)
    {
        Console.WriteLine("  Warning: Could not fetch MSRC data for CNA information");
        return cves;
    }

    var updatedCves = new List<Cve>();
    foreach (var cve in cves)
    {
        if (msrcData.TryGetValue(cve.Id, out var msrcCve))
        {
            var updatedCna = cve.Cna ?? new Cna("microsoft");
            bool hasUpdates = false;

            // Add severity if available
            if (!string.IsNullOrEmpty(msrcCve.CnaSeverity))
            {
                Console.WriteLine($"  Adding severity for {cve.Id}: {msrcCve.CnaSeverity}");
                updatedCna = updatedCna with { Severity = msrcCve.CnaSeverity };
                hasUpdates = true;
            }

            // Add impact if available
            if (!string.IsNullOrEmpty(msrcCve.Impact))
            {
                Console.WriteLine($"  Adding impact for {cve.Id}: {msrcCve.Impact}");
                updatedCna = updatedCna with { Impact = msrcCve.Impact };
                hasUpdates = true;
            }

            // Add acknowledgments if available
            if (msrcCve.Acknowledgments is not null && msrcCve.Acknowledgments.Count > 0)
            {
                Console.WriteLine($"  Adding {msrcCve.Acknowledgments.Count} acknowledgment(s) for {cve.Id}");
                updatedCna = updatedCna with { Acknowledgments = msrcCve.Acknowledgments };
                hasUpdates = true;
            }

            // Add FAQs if available
            if (msrcCve.Faqs is not null && msrcCve.Faqs.Count > 0)
            {
                Console.WriteLine($"  Adding {msrcCve.Faqs.Count} FAQ(s) for {cve.Id}");
                updatedCna = updatedCna with { Faq = msrcCve.Faqs };
                hasUpdates = true;
            }

            if (hasUpdates)
            {
                updatedCves.Add(cve with { Cna = updatedCna });
            }
            else
            {
                updatedCves.Add(cve);
            }
        }
        else
        {
            updatedCves.Add(cve);
        }
    }

    return updatedCves;
}

static async Task<bool> UpdateCveFile(string filePath, bool skipUrls)
{
    try
    {
        Console.WriteLine($"Updating: {filePath}");
        
        using var stream = File.OpenRead(filePath);
        var cveRecords = await CveUtils.GetCves(stream);
        
        if (cveRecords is null)
        {
            Console.WriteLine($"  ERROR: Failed to deserialize JSON");
            return false;
        }

        var generated = GenerateDictionaries(cveRecords);
        
        // Update cve_commits dictionary
        var cveCommits = GenerateCveCommits(cveRecords);
        
        // Fetch and update CVSS scores from CVE.org
        var updatedCves = await UpdateCvssScores(cveRecords.Disclosures);
        
        // Fetch CNA data from MSRC (unless --skip-urls is specified)
        if (!skipUrls)
        {
            updatedCves = await UpdateCnaDataFromMsrc(filePath, updatedCves);
        }
        
        // Create new record with updated dictionaries
        var updated = cveRecords with
        {
            Disclosures = updatedCves,
            CveReleases = generated.CveReleases,
            ProductCves = generated.ProductCves,
            PackageCves = generated.PackageCves,
            ProductName = generated.ProductName,
            ReleaseCves = generated.ReleaseCves,
            CveCommits = cveCommits
        };

        // Serialize with 2-space indentation to match original format
        string json = JsonSerializer.Serialize(updated, CveSerializerContext.Default.CveRecords);
        await File.WriteAllTextAsync(filePath, json);

        Console.WriteLine($"  ✓ Updated dictionaries, cve_commits, and CVSS scores");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}");
        return false;
    }
}

static void ValidateTaxonomy(CveRecords cves, List<string> errors)
{
    // Validate products
    if (cves.Products is not null)
    {
        var validProducts = new[] { "dotnet-runtime", "dotnet-aspnetcore", "dotnet-windows-desktop", "dotnet-sdk" };
        foreach (var product in cves.Products)
        {
            if (!validProducts.Contains(product.Name, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Invalid product name: '{product.Name}'");
            }
        }
    }

    // Validate platforms
    if (cves.Disclosures is not null)
    {
        var validPlatforms = new[] { "linux", "macos", "windows", "all" };
        foreach (var cve in cves.Disclosures)
        {
            if (cve.Platforms is not null)
            {
                foreach (var platform in cve.Platforms)
                {
                    if (!validPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"Invalid platform in {cve.Id}: '{platform}'");
                    }
                }
            }

            // Validate architectures
            if (cve.Architectures is not null)
            {
                var validArchitectures = new[] { "arm", "arm64", "x64", "x86", "all" };
                foreach (var arch in cve.Architectures)
                {
                    if (!validArchitectures.Contains(arch, StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"Invalid architecture in {cve.Id}: '{arch}'");
                    }
                }
            }

            // Validate severity
            if (!string.IsNullOrEmpty(cve.Cvss.Severity))
            {
                var validSeverities = new[] { "critical", "high", "medium", "low" };
                if (!validSeverities.Contains(cve.Cvss.Severity, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Invalid severity in {cve.Id}: '{cve.Cvss.Severity}'");
                }
            }

            // Validate CNA
            if (cve.Cna is not null)
            {
                var validCnas = new[] { "microsoft" };
                if (!validCnas.Any(v => string.Equals(v, cve.Cna.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add($"Invalid CNA in {cve.Id}: '{cve.Cna.Name}'");
                }
            }
        }
    }
}

static void ValidateProblemDescriptionMatch(CveRecords cves, List<string> errors)
{
    if (cves.Disclosures is null)
        return;

    // Map of vulnerability types that should be present in the description
    var vulnerabilityKeywords = new Dictionary<string, string[]>
    {
        ["denial of service"] = new[] { 
            "denial-of-service", "denial of service", "dos", "hang", "crash", "unresponsive",
            "allocate", "memory leak", "resource exhaustion", "exhaust"
        },
        ["remote code execution"] = new[] { 
            "remote code execution", "rce", "execute code", "arbitrary code", "run code",
            "use-after-free", "race condition", "specially crafted request", "specially crafted file",
            "exploit"
        },
        ["elevation of privilege"] = new[] { 
            "elevation of privilege", "eop", "privilege escalation", "escalate privileges", "elevated privileges",
            "local system", "system context", "administrator", "gain access"
        },
        ["information disclosure"] = new[] { 
            "information disclosure", "information leak", "data leak", "expose information", "disclosed", "reveal",
            "aitm", "adversary-in-the-middle", "mitm", "man-in-the-middle", "man in the middle",
            "steal", "intercept", "eavesdrop"
        },
        ["security feature bypass"] = new[] { 
            "security feature bypass", "bypass", "circumvent"
        },
        ["spoofing"] = new[] { 
            "spoofing", "spoof", "impersonation", "masquerade"
        },
        ["tampering"] = new[] { 
            "tampering", "tamper", "modify", "alter"
        }
    };

    foreach (var cve in cves.Disclosures)
    {
        var problem = cve.Problem.ToLowerInvariant();
        var description = string.Join(" ", cve.Description).ToLowerInvariant();

        // Skip if description is empty
        if (string.IsNullOrWhiteSpace(description))
            continue;

        // Extract the vulnerability type from the problem field
        string? detectedProblemType = null;
        foreach (var vulnType in vulnerabilityKeywords.Keys)
        {
            if (problem.Contains(vulnType))
            {
                detectedProblemType = vulnType;
                break;
            }
        }

        if (detectedProblemType is null)
            continue;

        // Check if any of the keywords for this vulnerability type appear in the description
        var keywords = vulnerabilityKeywords[detectedProblemType];
        bool foundMatch = keywords.Any(keyword => description.Contains(keyword));

        if (!foundMatch)
        {
            errors.Add($"{cve.Id}: Problem/Description mismatch - Problem states '{detectedProblemType}' but description does not mention related terms");
            errors.Add($"    Problem: {cve.Problem}");
            errors.Add($"    Description: {string.Join(" ", cve.Description)}");
        }

        // Check for deprecated MITM term and suggest AiTM
        if (description.Contains("mitm") || description.Contains("man-in-the-middle") || description.Contains("man in the middle"))
        {
            errors.Add($"{cve.Id}: Uses deprecated MITM (man-in-the-middle) term - consider using AiTM (adversary-in-the-middle) instead");
        }
    }
}

static void ValidateVersionCoherence(CveRecords cves, List<string> errors)
{
    // Validate products
    if (cves.Products is not null)
    {
        foreach (var product in cves.Products)
        {
            if (!IsVersionCoherent(product.MinVulnerable, product.MaxVulnerable, product.Fixed))
            {
                errors.Add($"Incoherent versions for {product.CveId} in product {product.Name}: min={product.MinVulnerable}, max={product.MaxVulnerable}, fixed={product.Fixed}");
            }
        }
    }

    // Validate packages
    if (cves.Packages is not null)
    {
        foreach (var package in cves.Packages)
        {
            if (!IsVersionCoherent(package.MinVulnerable, package.MaxVulnerable, package.Fixed))
            {
                errors.Add($"Incoherent versions for {package.CveId} in package {package.Name}: min={package.MinVulnerable}, max={package.MaxVulnerable}, fixed={package.Fixed}");
            }
        }
    }
}

static void ValidateReleaseFields(CveRecords cves, List<string> errors)
{
    // Validate products have non-empty release (required for products only)
    if (cves.Products is not null)
    {
        foreach (var product in cves.Products)
        {
            if (string.IsNullOrEmpty(product.Release))
            {
                errors.Add($"Product '{product.Name}' for {product.CveId} has null or empty release field (required)");
            }
        }
    }

    // Note: release field is optional for packages
}

static void ValidateReleaseVersionFormats(CveRecords cves, List<string> errors)
{
    // Validate products have two-part versions (e.g., "9.0")
    if (cves.Products is not null)
    {
        foreach (var product in cves.Products)
        {
            if (!string.IsNullOrEmpty(product.Release) && !IsTwoPartVersion(product.Release))
            {
                errors.Add($"Product '{product.Name}' for {product.CveId} has invalid release version '{product.Release}' (must be two-part version like '9.0')");
            }
        }
    }

    // Note: packages can have multi-part versions (more relaxed)
}

static bool IsTwoPartVersion(string version)
{
    // Split by dot and check that we have exactly two parts
    var parts = version.Split('.');
    if (parts.Length != 2)
        return false;

    // Each part should be a valid integer
    return int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _);
}

static void ValidateCommitBranchMatch(CveRecords cves, List<string> errors)
{
    if (cves.Commits is null)
        return;

    // Group products/packages by CVE to provide better error messages
    var cveProductsMap = new Dictionary<string, List<(string name, string release, IList<string> commits)>>();
    
    if (cves.Products is not null)
    {
        foreach (var product in cves.Products)
        {
            if (!string.IsNullOrEmpty(product.Release) && product.Commits is not null && product.Commits.Count > 0)
            {
                if (!cveProductsMap.ContainsKey(product.CveId))
                {
                    cveProductsMap[product.CveId] = new List<(string, string, IList<string>)>();
                }
                cveProductsMap[product.CveId].Add((product.Name, product.Release, product.Commits));
            }
        }
    }

    if (cves.Packages is not null)
    {
        foreach (var package in cves.Packages)
        {
            if (!string.IsNullOrEmpty(package.Release) && package.Commits is not null && package.Commits.Count > 0)
            {
                if (!cveProductsMap.ContainsKey(package.CveId))
                {
                    cveProductsMap[package.CveId] = new List<(string, string, IList<string>)>();
                }
                cveProductsMap[package.CveId].Add((package.Name, package.Release, package.Commits));
            }
        }
    }

    // Check each CVE
    foreach (var kvp in cveProductsMap)
    {
        var cveId = kvp.Key;
        var items = kvp.Value;
        
        var mismatches = new List<string>();
        var correctMatches = new List<string>();

        foreach (var (name, release, commits) in items)
        {
            foreach (var commitHash in commits)
            {
                if (cves.Commits.TryGetValue(commitHash, out var commitInfo))
                {
                    var expectedBranch = $"release/{release}";
                    if (!commitInfo.Branch.Equals(expectedBranch, StringComparison.OrdinalIgnoreCase))
                    {
                        mismatches.Add($"    {name} (release {release}) uses commit {commitHash} from branch '{commitInfo.Branch}'");
                    }
                    else
                    {
                        correctMatches.Add($"    {name} (release {release}) correctly uses commit {commitHash} from branch '{commitInfo.Branch}'");
                    }
                }
            }
        }

        // Only report error if there are mismatches
        if (mismatches.Count > 0)
        {
            errors.Add($"Commit branch mismatch for {cveId}:");
            foreach (var mismatch in mismatches)
            {
                errors.Add(mismatch);
            }
            
            // Show which releases have correct commits for context
            if (correctMatches.Count > 0)
            {
                errors.Add($"    Note: Other releases have correct branch commits:");
                foreach (var match in correctMatches)
                {
                    errors.Add(match);
                }
            }
        }
    }
}

static bool IsVersionCoherent(string minVersion, string maxVersion, string fixedVersion)
{
    try
    {
        var min = ParseSemVer(minVersion);
        var max = ParseSemVer(maxVersion);
        var fix = ParseSemVer(fixedVersion);

        // If any failed to parse, skip validation
        if (!min.HasValue || !max.HasValue || !fix.HasValue)
            return true;

        // Check: min <= max < fixed
        return CompareSemVer(min.Value, max.Value) <= 0 && CompareSemVer(max.Value, fix.Value) < 0;
    }
    catch
    {
        // If version parsing fails, skip this check
        return true;
    }
}

static (Version version, string? prerelease)? ParseSemVer(string versionString)
{
    try
    {
        // Split into version and prerelease parts
        int dashIndex = versionString.IndexOf('-');
        string versionPart = dashIndex > 0 ? versionString[..dashIndex] : versionString;
        string? prerelease = dashIndex > 0 ? versionString[(dashIndex + 1)..] : null;

        if (!Version.TryParse(versionPart, out var version))
            return null;

        return (version, prerelease);
    }
    catch
    {
        return null;
    }
}

static int CompareSemVer((Version version, string? prerelease) a, (Version version, string? prerelease) b)
{
    // Compare version numbers first
    int versionCompare = a.version.CompareTo(b.version);
    if (versionCompare != 0)
        return versionCompare;

    // If versions are equal, compare prerelease tags
    // Per semver: 1.0.0-alpha < 1.0.0-beta < 1.0.0-rc < 1.0.0
    // (release version is greater than any prerelease)

    if (a.prerelease is null && b.prerelease is null)
        return 0;
    if (a.prerelease is null) // a is release, b is prerelease
        return 1;
    if (b.prerelease is null) // a is prerelease, b is release
        return -1;

    // Both have prerelease tags - compare them lexicographically
    // This handles: preview < rc, and preview.1 < preview.2, rc.1 < rc.2
    return string.Compare(a.prerelease, b.prerelease, StringComparison.OrdinalIgnoreCase);
}

static void ValidateForeignKeys(CveRecords cves, List<string> errors)
{
    // Collect all CVE IDs
    var cveIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (cves.Disclosures is not null)
    {
        foreach (var cve in cves.Disclosures)
        {
            cveIds.Add(cve.Id);
        }
    }

    // Collect all commit hashes
    var commitHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (cves.Commits is not null)
    {
        foreach (var commit in cves.Commits.Keys)
        {
            commitHashes.Add(commit);
        }
    }

    // Check products reference valid CVEs
    if (cves.Products is not null)
    {
        foreach (var product in cves.Products)
        {
            if (!cveIds.Contains(product.CveId))
            {
                errors.Add($"Product '{product.Name}' references unknown CVE: {product.CveId}");
            }

            // If commits exist in the file, products must have non-null, non-empty commits
            if (cves.Commits is not null)
            {
                if (product.Commits is null)
                {
                    errors.Add($"Product '{product.Name}' for {product.CveId} has null commits (expected non-empty array)");
                }
                else if (product.Commits.Count == 0)
                {
                    errors.Add($"Product '{product.Name}' for {product.CveId} has empty commits array (expected at least one commit)");
                }
                else
                {
                    // Check for empty strings in commits array
                    foreach (var commit in product.Commits)
                    {
                        if (string.IsNullOrWhiteSpace(commit))
                        {
                            errors.Add($"Product '{product.Name}' for {product.CveId} has empty or whitespace commit hash");
                        }
                    }
                }
            }
        }
    }

    // Check packages reference valid CVEs
    if (cves.Packages is not null)
    {
        foreach (var package in cves.Packages)
        {
            if (!cveIds.Contains(package.CveId))
            {
                errors.Add($"Package '{package.Name}' references unknown CVE: {package.CveId}");
            }

            // If commits exist in the file, packages must have non-null, non-empty commits
            if (cves.Commits is not null)
            {
                if (package.Commits is null)
                {
                    errors.Add($"Package '{package.Name}' for {package.CveId} has null commits (expected non-empty array)");
                }
                else if (package.Commits.Count == 0)
                {
                    errors.Add($"Package '{package.Name}' for {package.CveId} has empty commits array (expected at least one commit)");
                }
                else
                {
                    // Check for empty strings in commits array
                    foreach (var commit in package.Commits)
                    {
                        if (string.IsNullOrWhiteSpace(commit))
                        {
                            errors.Add($"Package '{package.Name}' for {package.CveId} has empty or whitespace commit hash");
                        }
                    }
                }
            }
        }
    }

    // Check cve_commits references valid CVEs and commits
    if (cves.CveCommits is not null)
    {
        foreach (var kvp in cves.CveCommits)
        {
            if (!cveIds.Contains(kvp.Key))
            {
                errors.Add($"cve_commits references unknown CVE: {kvp.Key}");
            }

            foreach (var commitHash in kvp.Value)
            {
                if (!commitHashes.Contains(commitHash))
                {
                    errors.Add($"CVE {kvp.Key} references unknown commit: {commitHash}");
                }
            }
        }
    }

    // Check that each CVE is referenced at least once
    var referencedCves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (cves.Products is not null)
    {
        foreach (var product in cves.Products)
        {
            referencedCves.Add(product.CveId);
        }
    }
    if (cves.Packages is not null)
    {
        foreach (var package in cves.Packages)
        {
            referencedCves.Add(package.CveId);
        }
    }

    foreach (var cveId in cveIds)
    {
        if (!referencedCves.Contains(cveId))
        {
            errors.Add($"CVE {cveId} is not referenced by any product or package");
        }
    }

    // Check that each commit is referenced at least once
    var referencedCommits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (cves.CveCommits is not null)
    {
        foreach (var kvp in cves.CveCommits)
        {
            foreach (var commitHash in kvp.Value)
            {
                referencedCommits.Add(commitHash);
            }
        }
    }

    foreach (var commitHash in commitHashes)
    {
        if (!referencedCommits.Contains(commitHash))
        {
            errors.Add($"Commit {commitHash} is not referenced by any CVE");
        }
    }
}

static async Task ValidateNuGetPackages(CveRecords cves, List<string> errors)
{
    if (cves.Packages is null || cves.Packages.Count == 0)
        return;

    using var client = new HttpClient();
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "CveValidate/1.0");

    // Collect unique package names
    var packageNames = new HashSet<string>();
    foreach (var package in cves.Packages)
    {
        packageNames.Add(package.Name);
    }

    // Validate all packages in parallel
    var validationTasks = packageNames.Select(name => ValidateNuGetPackage(client, name)).ToArray();
    var results = await Task.WhenAll(validationTasks);

    // Collect all errors
    foreach (var error in results.Where(e => e is not null))
    {
        errors.Add(error!);
    }
}

static async Task<string?> ValidateNuGetPackage(HttpClient client, string packageName)
{
    // Skip validation for files (e.g., .so, .dll, .dylib files)
    if (packageName.Contains('.') && 
        (packageName.EndsWith(".so", StringComparison.OrdinalIgnoreCase) ||
         packageName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
         packageName.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase)))
    {
        return $"Package '{packageName}' appears to be a file (with extension), not a NuGet package";
    }

    try
    {
        // Use NuGet.org API v3 to check if package exists
        string url = $"https://api.nuget.org/v3/registration5-semver1/{packageName.ToLowerInvariant()}/index.json";
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Check if it matches the Microsoft.*Runtime pattern (appears to be a product not package)
            if (packageName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) && 
                packageName.EndsWith("Runtime", StringComparison.OrdinalIgnoreCase))
            {
                return $"Package '{packageName}' not found on nuget.org (appears to be a product not package)";
            }
            
            return $"Package '{packageName}' not found on nuget.org";
        }
        
        if (!response.IsSuccessStatusCode)
        {
            return $"Package '{packageName}' validation failed with status {(int)response.StatusCode}";
        }
        
        return null;
    }
    catch (HttpRequestException ex)
    {
        return $"Package '{packageName}' validation request failed: {ex.Message}";
    }
    catch (TaskCanceledException)
    {
        return $"Package '{packageName}' validation timeout";
    }
}

static async Task ValidateUrls(CveRecords cves, List<string> errors)
{
    using var client = new HttpClient();
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "CveValidate/1.0");

    var urls = new HashSet<string>();

    // Collect URLs from CVEs
    if (cves.Disclosures is not null)
    {
        foreach (var cve in cves.Disclosures)
        {
            if (cve.References is not null)
            {
                foreach (var url in cve.References)
                {
                    urls.Add(url);
                }
            }
        }
    }

    // Collect URLs from commits
    if (cves.Commits is not null)
    {
        foreach (var commit in cves.Commits.Values)
        {
            urls.Add(commit.Url);
        }
    }

    // Validate all URLs in parallel
    var validationTasks = urls.Select(url => ValidateSingleUrl(client, url)).ToArray();
    var results = await Task.WhenAll(validationTasks);

    // Collect all errors
    foreach (var error in results.Where(e => e is not null))
    {
        errors.Add(error!);
    }
}

static async Task<string?> ValidateSingleUrl(HttpClient client, string url)
{
    try
    {
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            return $"URL returned {(int)response.StatusCode}: {url}";
        }
        return null;
    }
    catch (HttpRequestException ex)
    {
        return $"URL request failed: {url} - {ex.Message}";
    }
    catch (TaskCanceledException)
    {
        return $"URL request timeout: {url}";
    }
}

static void ReportErrors(List<string> errors)
{
    foreach (var error in errors)
    {
        Console.WriteLine($"  ✗ {error}");
    }
}

static GeneratedDictionaries GenerateDictionaries(CveRecords cveRecords)
{
    var productName = new Dictionary<string, string>();
    var productCves = new Dictionary<string, List<string>>();
    var packageCves = new Dictionary<string, List<string>>();
    var cveReleases = new Dictionary<string, List<string>>();
    var releaseCves = new Dictionary<string, List<string>>();

    // Build a set of valid CVE IDs
    var validCveIds = new HashSet<string>(cveRecords.Disclosures.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);

    // Build product_name and product_cves from products ONLY
    foreach (var product in cveRecords.Products)
    {
        if (!productName.ContainsKey(product.Name))
        {
            productName[product.Name] = GetProductDisplayName(product.Name);
        }

        // Only add CVE if it exists in the cves property
        if (validCveIds.Contains(product.CveId))
        {
            if (!productCves.ContainsKey(product.Name))
            {
                productCves[product.Name] = new List<string>();
            }
            if (!productCves[product.Name].Contains(product.CveId))
            {
                productCves[product.Name].Add(product.CveId);
            }

            // Only process release mappings if release is not empty
            if (!string.IsNullOrEmpty(product.Release))
            {
                string release = product.Release;

                if (!cveReleases.ContainsKey(product.CveId))
                {
                    cveReleases[product.CveId] = new List<string>();
                }
                if (!cveReleases[product.CveId].Contains(release))
                {
                    cveReleases[product.CveId].Add(release);
                }

                if (!releaseCves.ContainsKey(release))
                {
                    releaseCves[release] = new List<string>();
                }
                if (!releaseCves[release].Contains(product.CveId))
                {
                    releaseCves[release].Add(product.CveId);
                }
            }
        }
    }

    // Build package_cves from packages (no need to add to product_name)
    foreach (var package in cveRecords.Packages)
    {
        // Only add CVE if it exists in the cves property
        if (validCveIds.Contains(package.CveId))
        {
            if (!packageCves.ContainsKey(package.Name))
            {
                packageCves[package.Name] = new List<string>();
            }
            if (!packageCves[package.Name].Contains(package.CveId))
            {
                packageCves[package.Name].Add(package.CveId);
            }

            // Only process release mappings if release is not empty
            if (!string.IsNullOrEmpty(package.Release))
            {
                string release = package.Release;

                if (!cveReleases.ContainsKey(package.CveId))
                {
                    cveReleases[package.CveId] = new List<string>();
                }
                if (!cveReleases[package.CveId].Contains(release))
                {
                    cveReleases[package.CveId].Add(release);
                }

                if (!releaseCves.ContainsKey(release))
                {
                    releaseCves[release] = new List<string>();
                }
                if (!releaseCves[release].Contains(package.CveId))
                {
                    releaseCves[release].Add(package.CveId);
                }
            }
        }
    }

    // Sort all lists for consistency
    foreach (var list in productCves.Values)
        list.Sort();
    foreach (var list in packageCves.Values)
        list.Sort();
    foreach (var list in cveReleases.Values)
        list.Sort();
    foreach (var list in releaseCves.Values)
        list.Sort();

    // Return dictionaries with sorted keys
    return new GeneratedDictionaries(
        CveReleases: cveReleases.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => (IList<string>)v.Value),
        ProductCves: productCves.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => (IList<string>)v.Value),
        PackageCves: packageCves.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => (IList<string>)v.Value),
        ProductName: productName.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => v.Value),
        ReleaseCves: releaseCves.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => (IList<string>)v.Value)
    );
}

static IDictionary<string, IList<string>> GenerateCveCommits(CveRecords cveRecords)
{
    var cveCommits = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    // Build a set of valid commit hashes and CVE IDs
    var validCommits = cveRecords.Commits is not null 
        ? new HashSet<string>(cveRecords.Commits.Keys, StringComparer.OrdinalIgnoreCase)
        : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var validCveIds = new HashSet<string>(cveRecords.Disclosures.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);

    // Collect commits from products
    foreach (var product in cveRecords.Products)
    {
        // Only process if CVE exists in cves property
        if (validCveIds.Contains(product.CveId) && product.Commits is not null && product.Commits.Count > 0)
        {
            if (!cveCommits.ContainsKey(product.CveId))
            {
                cveCommits[product.CveId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            foreach (var commit in product.Commits)
            {
                // Only add commit if it exists in commits property
                if (validCommits.Contains(commit))
                {
                    cveCommits[product.CveId].Add(commit);
                }
            }
        }
    }

    // Collect commits from packages
    foreach (var package in cveRecords.Packages)
    {
        // Only process if CVE exists in cves property
        if (validCveIds.Contains(package.CveId) && package.Commits is not null && package.Commits.Count > 0)
        {
            if (!cveCommits.ContainsKey(package.CveId))
            {
                cveCommits[package.CveId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            foreach (var commit in package.Commits)
            {
                // Only add commit if it exists in commits property
                if (validCommits.Contains(commit))
                {
                    cveCommits[package.CveId].Add(commit);
                }
            }
        }
    }

    // Convert to sorted dictionary with sorted lists
    return cveCommits
        .OrderBy(k => k.Key)
        .ToDictionary(
            k => k.Key,
            v => (IList<string>)v.Value.OrderBy(c => c).ToList()
        );
}

static string GetProductDisplayName(string productName)
{
    return productName switch
    {
        "dotnet-runtime-libraries" => ".NET Runtime Libraries",
        "dotnet-runtime-aspnetcore" => "ASP.NET Core Runtime",
        "dotnet-runtime" => ".NET Runtime Libraries",
        "dotnet-aspnetcore" => "ASP.NET Core Runtime",
        "dotnet-sdk" => ".NET SDK",
        "aspnetcore-runtime" => "ASP.NET Core Runtime",
        _ => productName
    };
}

static void ValidateDictionaries(CveRecords cveRecords, List<string> errors)
{
    var expected = GenerateDictionaries(cveRecords);

    // Validate cve_releases
    ValidateDictionary(cveRecords.CveReleases, expected.CveReleases, "cve_releases", errors);

    // Validate product_cves
    ValidateDictionary(cveRecords.ProductCves, expected.ProductCves, "product_cves", errors);

    // Validate package_cves
    ValidateDictionary(cveRecords.PackageCves, expected.PackageCves, "package_cves", errors);

    // Validate product_name
    ValidateDictionary(cveRecords.ProductName, expected.ProductName, "product_name", errors);

    // Validate release_cves
    ValidateDictionary(cveRecords.ReleaseCves, expected.ReleaseCves, "release_cves", errors);

    // Validate cve_commits
    var expectedCveCommits = GenerateCveCommits(cveRecords);
    if (expectedCveCommits.Count > 0)
    {
        ValidateDictionary(cveRecords.CveCommits, expectedCveCommits, "cve_commits", errors);
    }
}

static void ValidateDictionary<T>(
    IDictionary<string, T>? actual,
    IDictionary<string, T>? expected,
    string dictionaryName,
    List<string> errors)
{
    if (expected is null && actual is null)
        return;

    if (expected is null || actual is null)
    {
        errors.Add($"{dictionaryName}: Dictionary is {(actual is null ? "missing" : "unexpected")}");
        return;
    }

    // Check for missing keys
    foreach (var key in expected.Keys)
    {
        if (!actual.ContainsKey(key))
        {
            errors.Add($"{dictionaryName}: Missing key '{key}'");
        }
    }

    // Check for extra keys
    foreach (var key in actual.Keys)
    {
        if (!expected.ContainsKey(key))
        {
            errors.Add($"{dictionaryName}: Unexpected key '{key}'");
        }
    }

    // Check values for matching keys
    foreach (var key in expected.Keys.Intersect(actual.Keys))
    {
        var expectedValue = expected[key];
        var actualValue = actual[key];

        if (expectedValue is IList<string> expectedList && actualValue is IList<string> actualList)
        {
            var expectedSorted = expectedList.OrderBy(x => x).ToList();
            var actualSorted = actualList.OrderBy(x => x).ToList();

            if (!expectedSorted.SequenceEqual(actualSorted))
            {
                errors.Add($"{dictionaryName}['{key}']: Value mismatch");
                errors.Add($"    Expected: [{string.Join(", ", expectedSorted)}]");
                errors.Add($"    Actual:   [{string.Join(", ", actualSorted)}]");
            }
        }
        else if (!Equals(expectedValue, actualValue))
        {
            errors.Add($"{dictionaryName}['{key}']: Value mismatch");
            errors.Add($"    Expected: {expectedValue}");
            errors.Add($"    Actual:   {actualValue}");
        }
    }
}

static void ReportInvalidArgs()
{
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  CveValidate <command> <path> [options]");
    Console.WriteLine("  CveValidate <path> [options]              (defaults to validate)");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  validate    Validate cve.json file(s) including dictionaries");
    Console.WriteLine("  update      Update dictionaries and fetch CVSS scores from CVE.org");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  <path>      Path to a cve.json file or directory containing cve.json files");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --skip-urls Skip URL and MSRC validation/updates (faster, useful for offline)");
    Console.WriteLine("  --quiet, -q Only show files with errors (suppress success messages)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  CveValidate validate ~/git/core/release-notes/archives");
    Console.WriteLine("  CveValidate ~/git/core/release-notes/archives --skip-urls --quiet");
    Console.WriteLine("  CveValidate update ~/git/core/release-notes/archives/2024/01/cve.json");
    Console.WriteLine("  CveValidate update ~/git/core/release-notes/archives/2024/01/cve.json --skip-urls");
    Console.WriteLine("  CveValidate validate ~/git/core/release-notes/archives -q");
}

static async Task ValidateAgainstReleasesJson(string cveFilePath, CveRecords cves, List<string> errors)
{
    // CVE files are in timeline/{year}/{month}/cve.json
    // We need to find releases in releases.json that were released in this month
    // and validate that their CVE IDs match what's in the timeline cve.json
    
    // Parse the path to extract year/month and find release-notes root
    var parts = cveFilePath.Replace("\\", "/").Split('/');
    var timelineIndex = Array.FindIndex(parts, p => p == "timeline");
    
    if (timelineIndex == -1 || timelineIndex + 2 >= parts.Length)
    {
        // Not in a timeline directory structure, skip this validation
        return;
    }
    
    var releaseNotesRoot = string.Join("/", parts.Take(timelineIndex));
    var year = parts[timelineIndex + 1];
    var month = parts[timelineIndex + 2];
    
    if (!Directory.Exists(releaseNotesRoot))
    {
        errors.Add($"Cannot find release-notes root directory: {releaseNotesRoot}");
        return;
    }
    
    // For each major version in release_cves, check releases.json
    if (cves.ReleaseCves != null)
    {
        foreach (var majorVersion in cves.ReleaseCves.Keys)
        {
            // Skip SDK feature bands (e.g., "9.0.1xx")
            if (majorVersion.Contains('.') && majorVersion.EndsWith("xx"))
            {
                continue;
            }
            
            var releasesJsonPath = Path.Combine(releaseNotesRoot, majorVersion, "releases.json");
            if (!File.Exists(releasesJsonPath))
            {
                continue; // releases.json might not exist for all versions
            }
            
            try
            {
                var releasesJson = await File.ReadAllTextAsync(releasesJsonPath);
                var releasesDoc = JsonDocument.Parse(releasesJson);
                
                // Find releases that were released in this year/month
                if (releasesDoc.RootElement.TryGetProperty("releases", out var releasesArray))
                {
                    foreach (var release in releasesArray.EnumerateArray())
                    {
                        if (!release.TryGetProperty("release-date", out var releaseDateProp))
                        {
                            continue;
                        }
                        
                        var releaseDate = releaseDateProp.GetString();
                        if (string.IsNullOrEmpty(releaseDate) || !releaseDate.StartsWith($"{year}-{month}"))
                        {
                            continue; // Not released in this month
                        }
                        
                        // This release was released in this month, validate its CVEs
                        if (!release.TryGetProperty("release-version", out var versionProp))
                        {
                            continue;
                        }
                        
                        var patchVersion = versionProp.GetString();
                        if (string.IsNullOrEmpty(patchVersion))
                        {
                            continue;
                        }
                        
                        // Extract CVE IDs from this release
                        var cveIdsFromRelease = new HashSet<string>();
                        if (release.TryGetProperty("cve-list", out var cveListArray))
                        {
                            foreach (var cveEntry in cveListArray.EnumerateArray())
                            {
                                if (cveEntry.TryGetProperty("cve-id", out var cveIdProp))
                                {
                                    var cveId = cveIdProp.GetString();
                                    if (!string.IsNullOrEmpty(cveId))
                                    {
                                        cveIdsFromRelease.Add(cveId);
                                    }
                                }
                            }
                        }
                        
                        if (cveIdsFromRelease.Count == 0)
                        {
                            continue; // No CVEs in this release
                        }
                        
                        // Get CVE IDs from cve.json for this major version
                        var cveIdsFromCveJson = new HashSet<string>();
                        if (cves.ReleaseCves.TryGetValue(majorVersion, out var cvesForVersion))
                        {
                            foreach (var cveId in cvesForVersion)
                            {
                                cveIdsFromCveJson.Add(cveId);
                            }
                        }
                        
                        // Validate using shared logic
                        CveHandler.CveTransformer.ValidateCveData(patchVersion, cveIdsFromRelease.ToList(), cveIdsFromCveJson.ToList());
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error validating against {releasesJsonPath}: {ex.Message}");
            }
        }
    }
}

static async Task ValidateMsrcData(string filePath, CveRecords cves, List<string> errors)
{
    var msrcData = await FetchMsrcDataForFile(filePath);
    if (msrcData is null)
    {
        errors.Add("MSRC: Could not fetch MSRC data for this file");
        return;
    }

    foreach (var cve in cves.Disclosures)
    {
        if (msrcData.TryGetValue(cve.Id, out var msrcCve))
        {
            if (cve.Cvss.Score != msrcCve.Score)
            {
                errors.Add($"MSRC: {cve.Id} score mismatch - Expected: {msrcCve.Score}, Actual: {cve.Cvss.Score}");
            }
            
            if (cve.Cvss.Vector != msrcCve.Vector)
            {
                errors.Add($"MSRC: {cve.Id} vector mismatch");
            }

            if (cve.Weakness != msrcCve.Weakness)
            {
                errors.Add($"MSRC: {cve.Id} weakness/CWE mismatch");
            }

            // Validate CNA impact
            if (!string.IsNullOrEmpty(msrcCve.Impact))
            {
                var actualImpact = cve.Cna?.Impact;
                if (string.IsNullOrEmpty(actualImpact))
                {
                    errors.Add($"MSRC: {cve.Id} missing cna.impact - Expected: '{msrcCve.Impact}'");
                }
                else if (actualImpact != msrcCve.Impact)
                {
                    errors.Add($"MSRC: {cve.Id} cna.impact mismatch - Expected: '{msrcCve.Impact}', Actual: '{actualImpact}'");
                }
            }

            // Validate CNA severity
            if (!string.IsNullOrEmpty(msrcCve.CnaSeverity))
            {
                var actualSeverity = cve.Cna?.Severity;
                if (string.IsNullOrEmpty(actualSeverity))
                {
                    errors.Add($"MSRC: {cve.Id} missing cna.severity - Expected: '{msrcCve.CnaSeverity}'");
                }
                else if (actualSeverity != msrcCve.CnaSeverity)
                {
                    errors.Add($"MSRC: {cve.Id} cna.severity mismatch - Expected: '{msrcCve.CnaSeverity}', Actual: '{actualSeverity}'");
                }
            }
        }
    }
}

static async Task<Dictionary<string, MsrcCveData>?> FetchMsrcDataForFile(string filePath)
{
    // Extract year and month from file path (format: .../YYYY/MM/cve.json)
    var match = Regex.Match(filePath, @"(\d{4})/(\d{2})/cve\.json");
    if (!match.Success)
    {
        return null;
    }

    string year = match.Groups[1].Value;
    string month = match.Groups[2].Value;
    string monthName = new DateTime(int.Parse(year), int.Parse(month), 1).ToString("MMM");
    string msrcId = $"{year}-{monthName}";
    
    return await FetchMsrcData(msrcId);
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
    catch (Exception)
    {
        return null;
    }
}

static Dictionary<string, MsrcCveData> ParseMsrcXml(string xmlContent)
{
    var result = new Dictionary<string, MsrcCveData>();
    
    // Parse embedded HTML table from DocumentNotes
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
                    Impact = "",
                    Weakness = null,
                    CnaSeverity = null
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
        if (cveElem is null) continue;

        var cveId = cveElem.Value;
        if (!result.ContainsKey(cveId)) continue;

        // Get Impact
        var impactElem = vuln.Descendants(vulnNs + "Threat")
            .FirstOrDefault(t => t.Attribute("Type")?.Value == "Impact");
        if (impactElem is not null)
        {
            var impactDesc = impactElem.Element(vulnNs + "Description")?.Value ?? "";
            result[cveId] = result[cveId] with { Impact = impactDesc };
        }

        // Get CNA Severity
        var severityElem = vuln.Descendants(vulnNs + "Threat")
            .FirstOrDefault(t => t.Attribute("Type")?.Value == "Severity");
        if (severityElem is not null)
        {
            var severityDesc = severityElem.Element(vulnNs + "Description")?.Value ?? "";
            result[cveId] = result[cveId] with { CnaSeverity = severityDesc };
        }

        // Get CWE
        var cweElem = vuln.Element(vulnNs + "CWE");
        if (cweElem is not null)
        {
            var cweId = cweElem.Attribute("ID")?.Value;
            if (cweId is not null)
            {
                result[cveId] = result[cveId] with { Weakness = cweId };
            }
        }

        // Get Acknowledgments
        var acknowledgments = new List<string>();
        var acknowledgementsElem = vuln.Element(vulnNs + "Acknowledgments");
        if (acknowledgementsElem is not null)
        {
            foreach (var ackElem in acknowledgementsElem.Elements(vulnNs + "Acknowledgment"))
            {
                var nameElem = ackElem.Element(vulnNs + "Name");
                if (nameElem is not null)
                {
                    // Strip HTML tags from acknowledgment names
                    var name = Regex.Replace(nameElem.Value, @"<[^>]+>", "");
                    name = name.Replace("&amp;", "&").Trim();
                    if (!string.IsNullOrEmpty(name) && !acknowledgments.Contains(name))
                    {
                        acknowledgments.Add(name);
                    }
                }
            }
        }
        if (acknowledgments.Count > 0)
        {
            result[cveId] = result[cveId] with { Acknowledgments = acknowledgments };
        }

        // Get FAQs
        var faqs = new List<CnaFaq>();
        var notesElems = vuln.Elements(vulnNs + "Notes");
        foreach (var notesElem in notesElems)
        {
            var faqNotes = notesElem.Elements(vulnNs + "Note")
                .Where(n => n.Attribute("Type")?.Value == "FAQ");
            
            foreach (var faqNote in faqNotes)
            {
                var htmlContent = faqNote.Value;
                // Parse the FAQ HTML content
                var questionMatch = Regex.Match(htmlContent, @"<strong>(.*?)</strong>", RegexOptions.Singleline);
                var answerMatch = Regex.Match(htmlContent, @"</strong>\s*</p>\s*<p>(.*?)</p>", RegexOptions.Singleline);
                
                if (questionMatch.Success)
                {
                    var question = Regex.Replace(questionMatch.Groups[1].Value, @"<[^>]+>", "").Trim();
                    question = question.Replace("&amp;", "&");
                    
                    var answer = answerMatch.Success 
                        ? Regex.Replace(answerMatch.Groups[1].Value, @"<[^>]+>", "").Trim()
                        : "";
                    answer = answer.Replace("&amp;", "&");
                    
                    if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answer))
                    {
                        faqs.Add(new CnaFaq(question, answer));
                    }
                }
            }
        }
        if (faqs.Count > 0)
        {
            result[cveId] = result[cveId] with { Faqs = faqs };
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
    public List<string>? Acknowledgments { get; init; }
    public List<CnaFaq>? Faqs { get; init; }
}

record GeneratedDictionaries(
    IDictionary<string, IList<string>> CveReleases,
    IDictionary<string, IList<string>> ProductCves,
    IDictionary<string, IList<string>> PackageCves,
    IDictionary<string, string> ProductName,
    IDictionary<string, IList<string>> ReleaseCves
);
