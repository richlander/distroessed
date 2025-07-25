using System.Text.Json;
using DotnetRelease;

Console.WriteLine("Testing SDK lifecycle structure changes...");

// Create a sample PatchLifecycle 
var patchLifecycle = new PatchLifecycle(SupportPhase.Active, DateTimeOffset.Now);

// Create a sample SdkFeatureBandEntry
var featureBandEntry = new SdkFeatureBandEntry(
    ReleaseKind.Band,
    "8.0.1xx",
    ".NET SDK 8.0.1xx",
    new Dictionary<string, HalLink>
    {
        ["self"] = new HalLink("https://example.com/sdk/8.0.1xx")
        {
            Title = ".NET SDK 8.0.1xx",
            Type = "application/json"
        }
    })
{
    Lifecycle = patchLifecycle
};

// Create a sample SdkVersionIndex
var sdkIndex = new SdkVersionIndex(
    ReleaseKind.Index,
    "sdk",
    "8.0",
    ".NET SDK 8.0",
    new Dictionary<string, HalLink>
    {
        ["self"] = new HalLink("https://example.com/sdk/8.0/index.json")
        {
            Title = ".NET SDK 8.0",
            Type = "application/hal+json"
        }
    })
{
    Embedded = new SdkVersionIndexEmbedded(
        new List<SdkFeatureBandEntry> { featureBandEntry },
        new List<ReleaseVersionIndexEntry>()
    )
};

// Serialize to JSON to verify structure
var json = JsonSerializer.Serialize(sdkIndex, new JsonSerializerOptions { WriteIndented = true });

Console.WriteLine("Generated SDK Index Structure:");
Console.WriteLine("=".PadRight(50, '='));
Console.WriteLine(json);
Console.WriteLine("=".PadRight(50, '='));

// Verify no support-phase properties exist
if (json.Contains("support-phase"))
{
    Console.WriteLine("❌ ERROR: Found 'support-phase' property - should be removed!");
}
else
{
    Console.WriteLine("✅ SUCCESS: No 'support-phase' properties found");
}

// Verify lifecycle structure
if (json.Contains("\"phase\"") && json.Contains("\"release-date\"") && !json.Contains("\"eol-date\"") && !json.Contains("\"supported\""))
{
    Console.WriteLine("✅ SUCCESS: Lifecycle uses PatchLifecycle structure (phase + release-date only)");
}
else
{
    Console.WriteLine("❌ ERROR: Lifecycle structure incorrect");
}

Console.WriteLine("\nTest completed!");