using DotnetRelease;
using DotnetRelease.Summary;
using LlmsIndex;

// Generates llms.json - an AI-optimized index for LLM consumption
// - Latest patches for each supported release
// - Security status for the latest security month
// - Links to key navigation points in the graph

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: LlmsIndex <input-directory> [output-directory] [options]");
    Console.Error.WriteLine("  input-directory:  Directory containing release-notes data to read");
    Console.Error.WriteLine("  output-directory: Directory to write llms.json (optional, defaults to input-directory)");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options:");
    Console.Error.WriteLine("  --url-root <url>:    Base URL root (before /release-notes/) for generated links");
    Console.Error.WriteLine("                       Example: https://raw.githubusercontent.com/dotnet/core/commit-sha");
    Console.Error.WriteLine("  --output <filename>: Output filename (optional, defaults to llms.json)");
    Console.Error.WriteLine("  --workflows:         Include embedded workflows in output");
    return 1;
}

string? inputDir = null;
string? outputDir = null;
string? urlRoot = null;
string? outputFilename = null;
bool includeWorkflows = false;

// Parse arguments
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--url-root" && i + 1 < args.Length)
    {
        urlRoot = args[++i];
    }
    else if (args[i] == "--output" && i + 1 < args.Length)
    {
        outputFilename = args[++i];
    }
    else if (args[i] == "--workflows")
    {
        includeWorkflows = true;
    }
    else if (inputDir == null)
    {
        inputDir = args[i];
    }
    else if (outputDir == null)
    {
        outputDir = args[i];
    }
}

if (inputDir == null)
{
    Console.Error.WriteLine("Error: input-directory is required");
    return 1;
}

outputDir ??= inputDir;

if (!Directory.Exists(inputDir))
{
    Console.Error.WriteLine($"Input directory not found: {inputDir}");
    return 1;
}

// Set URL root if provided
if (urlRoot != null)
{
    Location.SetUrlRoot(urlRoot);
    Console.WriteLine($"Using URL root: {urlRoot}");
}

// Create output directory if it doesn't exist and it's different from input
if (inputDir != outputDir && !Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
    Console.WriteLine($"Created output directory: {outputDir}");
}

Console.WriteLine($"Input directory: {inputDir}");
if (inputDir != outputDir)
{
    Console.WriteLine($"Output directory: {outputDir}");
}

// Generate release summaries from source data
var summaries = await ReleaseSummaryLoader.GetReleaseSummariesAsync(inputDir)
    ?? throw new InvalidOperationException("Failed to generate release summaries.");

// Generate release history (for timeline data)
ReleaseHistory history = ReleaseSummaryLoader.GetReleaseCalendar(summaries);
ReleaseSummaryLoader.PopulateCveInformation(history, inputDir);

// Generate llms.json
await LlmsIndexFiles.GenerateAsync(inputDir, outputDir, summaries, history, includeWorkflows, outputFilename);

return 0;
