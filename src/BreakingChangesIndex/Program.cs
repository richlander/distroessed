using System.Diagnostics.CodeAnalysis;
using BreakingChangesIndex;

// Generates compatibility.json files from dotnet/docs compatibility documentation
// Data source: https://github.com/dotnet/docs/tree/main/docs/core/compatibility

[module: UnconditionalSuppressMessage("AOT", "IL3050", Justification = "This tool is not AOT compiled")]
[module: UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "This tool is not trimmed")]

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: BreakingChangesIndex <docs-compatibility-path> <output-dir> <version> [--schema <uri>]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Arguments:");
    Console.Error.WriteLine("  docs-compatibility-path  Path to docs/core/compatibility in dotnet/docs repo");
    Console.Error.WriteLine("  output-dir               Output directory (file will be <version>/compatibility.json)");
    Console.Error.WriteLine("  version                  .NET major version (e.g., 10.0, 9.0, 8.0)");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options:");
    Console.Error.WriteLine("  --schema <uri>           Schema URI to include in the output");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Example:");
    Console.Error.WriteLine("  BreakingChangesIndex ~/git/docs/docs/core/compatibility ~/git/core/release-notes 10.0");
    Console.Error.WriteLine("  # Creates ~/git/core/release-notes/10.0/compatibility.json");
    return 1;
}

string? docsPath = null;
string? outputDir = null;
string? version = null;
string? schemaUri = null;

// Parse arguments
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--schema" && i + 1 < args.Length)
    {
        schemaUri = args[++i];
    }
    else if (docsPath == null)
    {
        docsPath = args[i];
    }
    else if (outputDir == null)
    {
        outputDir = args[i];
    }
    else if (version == null)
    {
        version = args[i];
    }
}

if (docsPath == null || outputDir == null || version == null)
{
    Console.Error.WriteLine("Error: docs-compatibility-path, output-dir, and version are required");
    return 1;
}

if (!Directory.Exists(docsPath))
{
    Console.Error.WriteLine($"Error: Directory not found: {docsPath}");
    return 1;
}

// Construct output path: <output-dir>/<version>/compatibility.json
var outputPath = Path.Combine(outputDir, version, "compatibility.json");

// Ensure output directory exists
var outputFileDir = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(outputFileDir) && !Directory.Exists(outputFileDir))
{
    Directory.CreateDirectory(outputFileDir);
}

try
{
    await BreakingChangesGenerator.GenerateAsync(docsPath, outputPath, version, schemaUri);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
