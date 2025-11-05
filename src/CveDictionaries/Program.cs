using DotnetRelease.Cves;
using System.Text.Json;

// CveDictionaries - Validate and generate query dictionaries for cve.json files
// Usage:
//   CveDictionaries validate <path>    - Validate dictionaries in cve.json file(s)
//   CveDictionaries generate <path>    - Generate dictionaries for cve.json file(s)
//
// Examples:
//   CveDictionaries validate ~/git/core/release-notes/archives
//   CveDictionaries validate ~/git/core/release-notes/archives/2024/01/cve.json
//   CveDictionaries generate ~/git/core/release-notes/archives/2024/01/cve.json

const string jsonFilename = "cve.json";

Console.WriteLine("CveDictionaries");

if (args.Length < 2)
{
    PrintUsage();
    return 1;
}

string command = args[0].ToLowerInvariant();
string inputPath = args[1];

if (command != "validate" && command != "generate")
{
    Console.WriteLine($"Error: Invalid command '{args[0]}'. Must be 'validate' or 'generate'.");
    PrintUsage();
    return 1;
}

// Determine if input is a file or directory
List<string> cveFiles = new();

if (File.Exists(inputPath))
{
    if (!inputPath.EndsWith(jsonFilename))
    {
        Console.WriteLine($"Error: Input file must be named '{jsonFilename}'");
        PrintUsage();
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
    PrintUsage();
    return 1;
}

Console.WriteLine($"Found {cveFiles.Count} CVE file(s) to process");
Console.WriteLine();

int successCount = 0;
int failureCount = 0;

foreach (var cveFile in cveFiles)
{
    bool success = command == "validate" 
        ? await ValidateCveFile(cveFile) 
        : await GenerateCveFile(cveFile);
    
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

Console.WriteLine($"Processing complete: {successCount} succeeded, {failureCount} failed");
return failureCount > 0 ? 1 : 0;

async Task<bool> ValidateCveFile(string filePath)
{
    try
    {
        Console.WriteLine($"Validating: {filePath}");
        
        using var stream = File.OpenRead(filePath);
        var cveRecords = await CveUtils.GetCves(stream);
        
        if (cveRecords is null)
        {
            Console.WriteLine($"  ERROR: Failed to deserialize JSON");
            return false;
        }

        var expected = GenerateDictionaries(cveRecords);
        bool isValid = true;

        // Validate cve_releases
        if (!ValidateDictionary(cveRecords.CveReleases, expected.CveReleases, "cve_releases"))
            isValid = false;

        // Validate product_cves
        if (!ValidateDictionary(cveRecords.ProductCves, expected.ProductCves, "product_cves"))
            isValid = false;

        // Validate product_name
        if (!ValidateDictionary(cveRecords.ProductName, expected.ProductName, "product_name"))
            isValid = false;

        // Validate release_cves
        if (!ValidateDictionary(cveRecords.ReleaseCves, expected.ReleaseCves, "release_cves"))
            isValid = false;

        // Validate commits consistency
        if (!ValidateCommitsConsistency(cveRecords))
            isValid = false;

        if (isValid)
        {
            Console.WriteLine($"  ✓ All dictionaries are valid");
        }

        return isValid;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}");
        return false;
    }
}

async Task<bool> GenerateCveFile(string filePath)
{
    try
    {
        Console.WriteLine($"Generating: {filePath}");
        
        using var stream = File.OpenRead(filePath);
        var cveRecords = await CveUtils.GetCves(stream);
        
        if (cveRecords is null)
        {
            Console.WriteLine($"  ERROR: Failed to deserialize JSON");
            return false;
        }

        var generated = GenerateDictionaries(cveRecords);
        
        // Create new record with updated dictionaries
        var updated = cveRecords with
        {
            CveReleases = generated.CveReleases,
            ProductCves = generated.ProductCves,
            ProductName = generated.ProductName,
            ReleaseCves = generated.ReleaseCves
        };

        // Serialize with 2-space indentation to match original format
        string json = JsonSerializer.Serialize(updated, CveSerializerContext.Default.CveRecords);
        await File.WriteAllTextAsync(filePath, json);

        Console.WriteLine($"  ✓ Generated and saved dictionaries");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}");
        return false;
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
        // Add to product_name (using a standard mapping)
        if (!productName.ContainsKey(product.Name))
        {
            productName[product.Name] = GetProductDisplayName(product.Name);
        }

        // Add to product_cves
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

            // Add to cve_releases
            if (!cveReleases.ContainsKey(product.CveId))
            {
                cveReleases[product.CveId] = new List<string>();
            }
            if (!cveReleases[product.CveId].Contains(release))
            {
                cveReleases[product.CveId].Add(release);
            }

            // Add to release_cves
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
        // Add to product_name
        if (!productName.ContainsKey(package.Name))
        {
            productName[package.Name] = GetProductDisplayName(package.Name);
        }

        // Add to product_cves
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

            // Add to cve_releases
            if (!cveReleases.ContainsKey(package.CveId))
            {
                cveReleases[package.CveId] = new List<string>();
            }
            if (!cveReleases[package.CveId].Contains(release))
            {
                cveReleases[package.CveId].Add(release);
            }

            // Add to release_cves
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

static string GetProductDisplayName(string productName)
{
    // Map product names to display names
    return productName switch
    {
        "dotnet-runtime-libraries" => ".NET Runtime Libraries",
        "dotnet-runtime-aspnetcore" => "ASP.NET Core Runtime",
        "dotnet-runtime" => ".NET Runtime Libraries",
        "dotnet-aspnetcore" => "ASP.NET Core Runtime",
        "dotnet-sdk" => ".NET SDK",
        "aspnetcore-runtime" => "ASP.NET Core Runtime",
        _ => productName // Use the product name as-is if no mapping exists
    };
}

static bool ValidateDictionary<T>(
    IDictionary<string, T>? actual,
    IDictionary<string, T>? expected,
    string dictionaryName)
{
    if (expected is null && actual is null)
        return true;

    if (expected is null || actual is null)
    {
        Console.WriteLine($"  ✗ {dictionaryName}: Dictionary is {(actual is null ? "missing" : "unexpected")}");
        return false;
    }

    bool isValid = true;

    // Check for missing keys
    foreach (var key in expected.Keys)
    {
        if (!actual.ContainsKey(key))
        {
            Console.WriteLine($"  ✗ {dictionaryName}: Missing key '{key}'");
            isValid = false;
        }
    }

    // Check for extra keys
    foreach (var key in actual.Keys)
    {
        if (!expected.ContainsKey(key))
        {
            Console.WriteLine($"  ✗ {dictionaryName}: Unexpected key '{key}'");
            isValid = false;
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
                Console.WriteLine($"  ✗ {dictionaryName}['{key}']: Value mismatch");
                Console.WriteLine($"      Expected: [{string.Join(", ", expectedSorted)}]");
                Console.WriteLine($"      Actual:   [{string.Join(", ", actualSorted)}]");
                isValid = false;
            }
        }
        else if (!Equals(expectedValue, actualValue))
        {
            Console.WriteLine($"  ✗ {dictionaryName}['{key}']: Value mismatch");
            Console.WriteLine($"      Expected: {expectedValue}");
            Console.WriteLine($"      Actual:   {actualValue}");
            isValid = false;
        }
    }

    return isValid;
}

static bool ValidateCommitsConsistency(CveRecords cveRecords)
{
    // If commits dictionary is null or empty, no validation needed
    if (cveRecords.Commits is null || cveRecords.Commits.Count == 0)
    {
        return true;
    }

    bool isValid = true;

    // When commits exist, all products and packages should have non-null, non-empty commit arrays
    for (int i = 0; i < cveRecords.Products.Count; i++)
    {
        var product = cveRecords.Products[i];
        if (product.Commits is null)
        {
            Console.WriteLine($"  ✗ commits: products[{i}] ('{product.Name}' for {product.CveId}) has null commits (should be non-null when commits dictionary exists)");
            isValid = false;
        }
        else if (product.Commits.Count == 0)
        {
            Console.WriteLine($"  ✗ commits: products[{i}] ('{product.Name}' for {product.CveId}) has empty commits array (should reference commits or be populated)");
            isValid = false;
        }
    }

    for (int i = 0; i < cveRecords.Packages.Count; i++)
    {
        var package = cveRecords.Packages[i];
        if (package.Commits is null)
        {
            Console.WriteLine($"  ✗ commits: packages[{i}] ('{package.Name}' for {package.CveId}) has null commits (should be non-null when commits dictionary exists)");
            isValid = false;
        }
        else if (package.Commits.Count == 0)
        {
            Console.WriteLine($"  ✗ commits: packages[{i}] ('{package.Name}' for {package.CveId}) has empty commits array (should reference commits or be populated)");
            isValid = false;
        }
    }

    return isValid;
}

static void PrintUsage()
{
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  CveDictionaries <command> <path>");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  validate    Validate query dictionaries in cve.json file(s)");
    Console.WriteLine("  generate    Generate and update query dictionaries in cve.json file(s)");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  <path>      Path to a cve.json file or directory containing cve.json files");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  CveDictionaries validate ~/git/core/release-notes/archives");
    Console.WriteLine("  CveDictionaries validate ~/git/core/release-notes/archives/2024/01/cve.json");
    Console.WriteLine("  CveDictionaries generate ~/git/core/release-notes/archives/2024/01/cve.json");
}

record GeneratedDictionaries(
    IDictionary<string, IList<string>> CveReleases,
    IDictionary<string, IList<string>> ProductCves,
    IDictionary<string, string> ProductName,
    IDictionary<string, IList<string>> ReleaseCves
);
