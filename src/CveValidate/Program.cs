using System.Text.Json;
using CveInfo;
using DotnetRelease;

const string jsonFilename = "cve.json";

Console.WriteLine("CveValidate");

if (args.Length < 1)
{
    ReportInvalidArgs();
    return 1;
}

// Parse arguments
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
        inputPath = arg;
    }
}

if (inputPath is null)
{
    ReportInvalidArgs();
    return 1;
}

int totalFiles = 0;
int validFiles = 0;
int invalidFiles = 0;

// Determine if input is a file or directory
if (File.Exists(inputPath))
{
    if (!inputPath.EndsWith(jsonFilename))
    {
        Console.WriteLine($"Error: Input file must be named '{jsonFilename}'");
        return 1;
    }

    var result = await ValidateCveFile(inputPath, skipUrls);
    return result ? 0 : 1;
}
else if (Directory.Exists(inputPath))
{
    var cveFiles = Directory.GetFiles(inputPath, jsonFilename, SearchOption.AllDirectories);

    if (cveFiles.Length == 0)
    {
        Console.WriteLine($"No '{jsonFilename}' files found in directory: {inputPath}");
        return 1;
    }
    
    // Sort files alphabetically for consistent chronological order
    Array.Sort(cveFiles);

    Console.WriteLine($"Found {cveFiles.Length} CVE file(s) to validate");
    Console.WriteLine();

    foreach (var cveFile in cveFiles)
    {
        totalFiles++;
        bool isValid = await ValidateCveFile(cveFile, skipUrls);
        if (isValid)
        {
            validFiles++;
        }
        else
        {
            invalidFiles++;
        }
        Console.WriteLine();
    }

    Console.WriteLine($"Validation complete: {validFiles} valid, {invalidFiles} invalid");
    return invalidFiles > 0 ? 1 : 0;
}
else
{
    Console.WriteLine($"Error: Path not found: {inputPath}");
    return 1;
}

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

static void ReportInvalidArgs()
{
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  CveValidate <path> [--skip-urls]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  <path>       Path to a cve.json file or directory containing cve.json files");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --skip-urls  Skip URL validation (faster, useful for offline validation)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  CveValidate ~/git/core/release-notes/archives");
    Console.WriteLine("  CveValidate ~/git/core/release-notes/archives/2025/06/cve.json");
    Console.WriteLine("  CveValidate ~/git/core/release-notes/archives --skip-urls");
}
