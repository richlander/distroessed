using System.Text.Json;
using DotnetRelease;
using DotnetRelease.Cves;

// CveValidate - Validate and update CVE JSON files
// Usage:
//   CveValidate validate <path> [--skip-urls]    - Validate cve.json file(s)
//   CveValidate update <path>                    - Update dictionaries in cve.json file(s)
//
// Examples:
//   CveValidate validate ~/git/core/release-notes/archives
//   CveValidate validate ~/git/core/release-notes/archives/2024/01/cve.json --skip-urls
//   CveValidate update ~/git/core/release-notes/archives/2024/01/cve.json

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

foreach (var arg in args)
{
    if (arg == "--skip-urls")
    {
        skipUrls = true;
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
Console.WriteLine();

int successCount = 0;
int failureCount = 0;

foreach (var cveFile in cveFiles)
{
    bool success = command == "validate" 
        ? await ValidateCveFile(cveFile, skipUrls) 
        : await UpdateCveFile(cveFile);
    
    if (success)
    {
        successCount++;
    }
    else
    {
        failureCount++;
    }
    Console.WriteLine();
}

string action = command == "validate" ? "Validation" : "Update";
Console.WriteLine($"{action} complete: {successCount} succeeded, {failureCount} failed");
return failureCount > 0 ? 1 : 0;

static async Task<bool> ValidateCveFile(string filePath, bool skipUrls)
{
    Console.WriteLine($"Validating: {filePath}");
    var errors = new List<string>();

    try
    {
        // Load and deserialize the JSON
        using var jsonStream = File.OpenRead(filePath);
        var cves = await CveUtils.GetCves(jsonStream);

        if (cves is null)
        {
            errors.Add("Failed to deserialize JSON");
            ReportErrors(errors);
            return false;
        }

        // Run all validations
        ValidateTaxonomy(cves, errors);
        ValidateVersionCoherence(cves, errors);
        ValidateForeignKeys(cves, errors);
        ValidateDictionaries(cves, errors);
        await ValidateNuGetPackages(cves, errors);

        if (!skipUrls)
        {
            await ValidateUrls(cves, errors);
        }

        if (errors.Count == 0)
        {
            Console.WriteLine("  ✓ All validations passed");
            return true;
        }
        else
        {
            ReportErrors(errors);
            return false;
        }
    }
    catch (JsonException ex)
    {
        errors.Add($"JSON parsing error: {ex.Message}");
        ReportErrors(errors);
        return false;
    }
    catch (Exception ex)
    {
        errors.Add($"Unexpected error: {ex.Message}");
        ReportErrors(errors);
        return false;
    }
}

static async Task<bool> UpdateCveFile(string filePath)
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
        
        // Create new record with updated dictionaries
        var updated = cveRecords with
        {
            CveReleases = generated.CveReleases,
            ProductCves = generated.ProductCves,
            ProductName = generated.ProductName,
            ReleaseCves = generated.ReleaseCves,
            CveCommits = cveCommits
        };

        // Serialize with 2-space indentation to match original format
        string json = JsonSerializer.Serialize(updated, CveSerializerContext.Default.CveRecords);
        await File.WriteAllTextAsync(filePath, json);

        Console.WriteLine($"  ✓ Updated dictionaries and cve_commits");
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
    if (cves.Cves is not null)
    {
        var validPlatforms = new[] { "linux", "macos", "windows", "all" };
        foreach (var cve in cves.Cves)
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
            if (cve.Severity is not null)
            {
                var validSeverities = new[] { "critical", "high", "medium", "low" };
                if (!validSeverities.Contains(cve.Severity, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Invalid severity in {cve.Id}: '{cve.Severity}'");
                }
            }

            // Validate CNA
            if (cve.Cna is not null)
            {
                var validCnas = new[] { "microsoft" };
                if (!validCnas.Contains(cve.Cna, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Invalid CNA in {cve.Id}: '{cve.Cna}'");
                }
            }
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
    if (cves.Cves is not null)
    {
        foreach (var cve in cves.Cves)
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
    if (cves.Cves is not null)
    {
        foreach (var cve in cves.Cves)
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
    var cveReleases = new Dictionary<string, List<string>>();
    var releaseCves = new Dictionary<string, List<string>>();

    // Build product_name and product_cves from products
    foreach (var product in cveRecords.Products)
    {
        if (!productName.ContainsKey(product.Name))
        {
            productName[product.Name] = GetProductDisplayName(product.Name);
        }

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

    // Also process packages
    foreach (var package in cveRecords.Packages)
    {
        if (!productName.ContainsKey(package.Name))
        {
            productName[package.Name] = GetProductDisplayName(package.Name);
        }

        if (!productCves.ContainsKey(package.Name))
        {
            productCves[package.Name] = new List<string>();
        }
        if (!productCves[package.Name].Contains(package.CveId))
        {
            productCves[package.Name].Add(package.CveId);
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

    // Sort all lists for consistency
    foreach (var list in productCves.Values)
        list.Sort();
    foreach (var list in cveReleases.Values)
        list.Sort();
    foreach (var list in releaseCves.Values)
        list.Sort();

    // Return dictionaries with sorted keys
    return new GeneratedDictionaries(
        CveReleases: cveReleases.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => (IList<string>)v.Value),
        ProductCves: productCves.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => (IList<string>)v.Value),
        ProductName: productName.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => v.Value),
        ReleaseCves: releaseCves.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => (IList<string>)v.Value)
    );
}

static IDictionary<string, IList<string>> GenerateCveCommits(CveRecords cveRecords)
{
    var cveCommits = new Dictionary<string, HashSet<string>>();

    // Collect commits from products
    foreach (var product in cveRecords.Products)
    {
        if (product.Commits is not null && product.Commits.Count > 0)
        {
            if (!cveCommits.ContainsKey(product.CveId))
            {
                cveCommits[product.CveId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            foreach (var commit in product.Commits)
            {
                cveCommits[product.CveId].Add(commit);
            }
        }
    }

    // Collect commits from packages
    foreach (var package in cveRecords.Packages)
    {
        if (package.Commits is not null && package.Commits.Count > 0)
        {
            if (!cveCommits.ContainsKey(package.CveId))
            {
                cveCommits[package.CveId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            foreach (var commit in package.Commits)
            {
                cveCommits[package.CveId].Add(commit);
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
    Console.WriteLine("  CveValidate <command> <path> [--skip-urls]");
    Console.WriteLine("  CveValidate <path> [--skip-urls]              (defaults to validate)");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  validate    Validate cve.json file(s) including dictionaries");
    Console.WriteLine("  update      Update query dictionaries and cve_commits in cve.json file(s)");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  <path>      Path to a cve.json file or directory containing cve.json files");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --skip-urls Skip URL validation (faster, useful for offline validation)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  CveValidate validate ~/git/core/release-notes/archives");
    Console.WriteLine("  CveValidate ~/git/core/release-notes/archives --skip-urls");
    Console.WriteLine("  CveValidate update ~/git/core/release-notes/archives/2024/01/cve.json");
}

record GeneratedDictionaries(
    IDictionary<string, IList<string>> CveReleases,
    IDictionary<string, IList<string>> ProductCves,
    IDictionary<string, string> ProductName,
    IDictionary<string, IList<string>> ReleaseCves
);
