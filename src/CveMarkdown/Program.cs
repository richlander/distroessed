using DotnetRelease;
using MarkdownHelpers;

// Format is inspired by https://security.alpinelinux.org/vuln/CVE-2024-5535
// Usage:
// CveMarkdown <directory-path> <template-path> [--skip-existing]
// CveMarkdown ~/git/core/release-notes/archives ~/git/distroessed/templates/cve-template.md
// CveMarkdown ~/git/core/release-notes/archives ~/git/distroessed/templates/cve-template.md --skip-existing
// CveMarkdown ~/git/core/release-notes/archives/2025/01-January/cve.json ~/git/distroessed/templates/cve-template.md

// Static strings
const string jsonFilename = "cve.json";
const string targetFilename = "cve.md";
const string templateFilename = "cve-template.md";

Console.WriteLine("CveMarkdown");

if (args.Length < 2)
{
    ReportInvalidArgs();
    return;
}

string inputPath = args[0];
string templatePath = args[1];
bool skipExisting = args.Length > 2 && args[2] == "--skip-existing";

// Validate template exists
if (!File.Exists(templatePath) || !templatePath.EndsWith(templateFilename))
{
    Console.WriteLine($"Error: Template file not found or invalid: {templatePath}");
    Console.WriteLine($"Template must be named '{templateFilename}'");
    ReportInvalidArgs();
    return;
}

// Determine if input is a file or directory
if (File.Exists(inputPath))
{
    // Single file mode
    if (!inputPath.EndsWith(jsonFilename))
    {
        Console.WriteLine($"Error: Input file must be named '{jsonFilename}'");
        ReportInvalidArgs();
        return;
    }

    await ProcessCveFile(inputPath, templatePath, skipExisting);
}
else if (Directory.Exists(inputPath))
{
    // Directory mode - recursively find all cve.json files
    var cveFiles = Directory.GetFiles(inputPath, jsonFilename, SearchOption.AllDirectories);

    if (cveFiles.Length == 0)
    {
        Console.WriteLine($"No '{jsonFilename}' files found in directory: {inputPath}");
        return;
    }

    Console.WriteLine($"Found {cveFiles.Length} CVE file(s) to process");
    Console.WriteLine();

    int successCount = 0;
    int failureCount = 0;
    int skippedCount = 0;

    foreach (var cveFile in cveFiles)
    {
        var result = await ProcessCveFile(cveFile, templatePath, skipExisting);
        if (result == ProcessResult.Success)
        {
            successCount++;
        }
        else if (result == ProcessResult.Skipped)
        {
            skippedCount++;
        }
        else
        {
            failureCount++;
        }
        Console.WriteLine();
    }

    Console.WriteLine($"Processing complete: {successCount} succeeded, {skippedCount} skipped, {failureCount} failed");
}
else
{
    Console.WriteLine($"Error: Path not found: {inputPath}");
    ReportInvalidArgs();
    return;
}

static async Task<ProcessResult> ProcessCveFile(string sourceFile, string templatePath, bool skipExisting)
{
    try
    {
        string directory = Path.GetDirectoryName(sourceFile)!;
        string targetFile = Path.Combine(directory, targetFilename);

        // Check if target file already exists and we should skip
        if (skipExisting && File.Exists(targetFile))
        {
            Console.WriteLine($"Skipping: {sourceFile}");
            Console.WriteLine($"  Target already exists: {targetFile}");
            return ProcessResult.Skipped;
        }

        Console.WriteLine($"Processing: {sourceFile}");

        using var jsonStream = File.OpenRead(sourceFile);
        var cves = await CveUtils.GetCves(jsonStream);

        if (cves?.Cves is null)
        {
            Console.WriteLine($"  ERROR: JSON deserialization failed");
            return ProcessResult.Failed;
        }

        using var templateStream = File.OpenRead(templatePath);
        using var templateReader = new StreamReader(templateStream);
        using var targetStream = File.Open(targetFile, FileMode.Create);
        using var targetWriter = new StreamWriter(targetStream);

        MarkdownTemplate notes = CveReport.CreateTemplate(cves);
        notes.Process(templateReader, targetWriter);

        targetWriter.Close();

        var fileInfo = new FileInfo(targetFile);
        Console.WriteLine($"  Generated: {targetFile}");
        Console.WriteLine($"  Size: {fileInfo.Length:N0} bytes");

        return ProcessResult.Success;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}");
        return ProcessResult.Failed;
    }
}

static void ReportInvalidArgs()
{
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  CveMarkdown <path> <template-file> [--skip-existing]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  <path>           Path to a cve.json file or directory containing cve.json files");
    Console.WriteLine("  <template-file>  Path to the cve-template.md file");
    Console.WriteLine("  --skip-existing  Skip processing if cve.md already exists (optional)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  CveMarkdown ~/git/core/release-notes/archives ~/templates/cve-template.md");
    Console.WriteLine("  CveMarkdown ~/git/core/release-notes/archives ~/templates/cve-template.md --skip-existing");
    Console.WriteLine("  CveMarkdown ~/git/core/release-notes/archives/2025/01-January/cve.json ~/templates/cve-template.md");
}

enum ProcessResult
{
    Success,
    Skipped,
    Failed
}
