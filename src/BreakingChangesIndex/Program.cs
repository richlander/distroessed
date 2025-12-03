using BreakingChangesIndex;

// Generates breaking-changes.json files from dotnet/docs compatibility documentation
// Data source: https://github.com/dotnet/docs/tree/main/docs/core/compatibility

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: BreakingChangesIndex <docs-compatibility-path> <output-path> <version> [--schema <uri>]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Arguments:");
    Console.Error.WriteLine("  docs-compatibility-path  Path to docs/core/compatibility in dotnet/docs repo");
    Console.Error.WriteLine("  output-path              Path for the generated breaking-changes.json file");
    Console.Error.WriteLine("  version                  .NET major version (e.g., 10.0, 9.0, 8.0)");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options:");
    Console.Error.WriteLine("  --schema <uri>           Schema URI to include in the output");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Example:");
    Console.Error.WriteLine("  BreakingChangesIndex ~/git/docs/docs/core/compatibility ~/git/core/release-notes/10.0/breaking-changes.json 10.0");
    return 1;
}

string? docsPath = null;
string? outputPath = null;
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
    else if (outputPath == null)
    {
        outputPath = args[i];
    }
    else if (version == null)
    {
        version = args[i];
    }
}

if (docsPath == null || outputPath == null || version == null)
{
    Console.Error.WriteLine("Error: docs-compatibility-path, output-path, and version are required");
    return 1;
}

if (!Directory.Exists(docsPath))
{
    Console.Error.WriteLine($"Error: Directory not found: {docsPath}");
    return 1;
}

// Ensure output directory exists
var outputDir = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
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
