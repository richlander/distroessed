using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

if (args.Length < 2)
{
    Console.WriteLine("Usage: SourceLinkExtractor <package-name> <version>");
    Console.WriteLine("Example: SourceLinkExtractor Newtonsoft.Json 13.0.3");
    return 1;
}

string packageName = args[0].ToLowerInvariant();
string version = args[1].ToLowerInvariant();

using HttpClient client = new();
string tempDir = Path.Combine(Path.GetTempPath(), $"nuget-{packageName}-{version}-{Guid.NewGuid():N}");

// SourceLink GUID: CC110556-A091-4D38-9FEC-25AB9A351A6A
Guid sourceLinkGuid = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");

try
{
    Directory.CreateDirectory(tempDir);
    string extractPath = Path.Combine(tempDir, "extracted");

    // First try to download and extract .snupkg (symbol package)
    string snupkgUrl = $"https://api.nuget.org/v3-flatcontainer/{packageName}/{version}/{packageName}.{version}.snupkg";
    Console.WriteLine($"Trying symbol package: {snupkgUrl}");

    bool hasSnupkg = false;
    try
    {
        byte[] snupkgBytes = await client.GetByteArrayAsync(snupkgUrl);
        string snupkgPath = Path.Combine(tempDir, $"{packageName}.{version}.snupkg");
        await File.WriteAllBytesAsync(snupkgPath, snupkgBytes);
        ZipFile.ExtractToDirectory(snupkgPath, extractPath);
        hasSnupkg = true;
        Console.WriteLine("Symbol package downloaded successfully.");
    }
    catch (HttpRequestException)
    {
        Console.WriteLine("No symbol package available.");
    }

    // Also download the main .nupkg to check for embedded PDBs
    string nupkgUrl = $"https://api.nuget.org/v3-flatcontainer/{packageName}/{version}/{packageName}.{version}.nupkg";
    Console.WriteLine($"Downloading main package: {nupkgUrl}");

    byte[] packageBytes = await client.GetByteArrayAsync(nupkgUrl);
    string nupkgExtractPath = hasSnupkg ? Path.Combine(tempDir, "nupkg") : extractPath;
    Directory.CreateDirectory(nupkgExtractPath);

    string nupkgPath = Path.Combine(tempDir, $"{packageName}.{version}.nupkg");
    await File.WriteAllBytesAsync(nupkgPath, packageBytes);
    ZipFile.ExtractToDirectory(nupkgPath, nupkgExtractPath);

    Console.WriteLine();

    // Find standalone PDB files
    string[] pdbFiles = Directory.GetFiles(extractPath, "*.pdb", SearchOption.AllDirectories);
    bool foundSourceLink = false;

    if (pdbFiles.Length > 0)
    {
        Console.WriteLine($"Found {pdbFiles.Length} PDB file(s):\n");

        foreach (string pdbFile in pdbFiles)
        {
            string relativePath = Path.GetRelativePath(extractPath, pdbFile);
            Console.WriteLine($"=== {relativePath} ===");

            try
            {
                string? sourceLink = ExtractSourceLinkFromPdb(pdbFile, sourceLinkGuid);
                if (sourceLink is not null)
                {
                    PrintSourceLink(sourceLink);
                    foundSourceLink = true;
                }
                else
                {
                    Console.WriteLine("No SourceLink document found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading PDB: {ex.Message}");
            }
            Console.WriteLine();
        }
    }

    // Check DLLs for embedded PDBs
    string[] dllFiles = Directory.GetFiles(nupkgExtractPath, "*.dll", SearchOption.AllDirectories);

    if (dllFiles.Length > 0)
    {
        Console.WriteLine($"Checking {dllFiles.Length} DLL(s) for embedded PDBs:\n");

        foreach (string dllFile in dllFiles)
        {
            string relativePath = Path.GetRelativePath(nupkgExtractPath, dllFile);

            try
            {
                string? sourceLink = ExtractSourceLinkFromDll(dllFile, sourceLinkGuid);
                if (sourceLink is not null)
                {
                    Console.WriteLine($"=== {relativePath} (embedded PDB) ===");
                    PrintSourceLink(sourceLink);
                    foundSourceLink = true;
                    Console.WriteLine();
                }
            }
            catch
            {
                // Not all DLLs have embedded PDBs, ignore errors
            }
        }
    }

    if (!foundSourceLink)
    {
        Console.WriteLine("No SourceLink documents found in package.");
        return 1;
    }

    return 0;
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Failed to download package: {ex.Message}");
    return 1;
}
finally
{
    if (Directory.Exists(tempDir))
    {
        try
        {
            Directory.Delete(tempDir, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

static void PrintSourceLink(string sourceLink)
{
    try
    {
        var doc = JsonDocument.Parse(sourceLink);
        string pretty = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(pretty);
    }
    catch
    {
        Console.WriteLine(sourceLink);
    }
}

static string? ExtractSourceLinkFromPdb(string pdbPath, Guid sourceLinkGuid)
{
    using FileStream stream = File.OpenRead(pdbPath);

    byte[] header = new byte[4];
    stream.ReadExactly(header, 0, 4);
    stream.Position = 0;

    // Portable PDB starts with "BSJB"
    if (header[0] == 'B' && header[1] == 'S' && header[2] == 'J' && header[3] == 'B')
    {
        return ExtractFromPortablePdb(stream, sourceLinkGuid);
    }

    Console.WriteLine("  (Windows PDB format - SourceLink not available via metadata API)");
    return null;
}

static string? ExtractSourceLinkFromDll(string dllPath, Guid sourceLinkGuid)
{
    using FileStream stream = File.OpenRead(dllPath);
    using PEReader peReader = new(stream);

    // Check for embedded portable PDB
    foreach (var entry in peReader.ReadDebugDirectory())
    {
        if (entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
        {
            using MetadataReaderProvider provider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
            MetadataReader reader = provider.GetMetadataReader();
            return ExtractSourceLinkFromReader(reader, sourceLinkGuid);
        }
    }

    return null;
}

static string? ExtractFromPortablePdb(Stream stream, Guid sourceLinkGuid)
{
    using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(stream);
    MetadataReader reader = provider.GetMetadataReader();
    return ExtractSourceLinkFromReader(reader, sourceLinkGuid);
}

static string? ExtractSourceLinkFromReader(MetadataReader reader, Guid sourceLinkGuid)
{
    foreach (CustomDebugInformationHandle handle in reader.CustomDebugInformation)
    {
        CustomDebugInformation info = reader.GetCustomDebugInformation(handle);
        Guid kind = reader.GetGuid(info.Kind);

        if (kind == sourceLinkGuid)
        {
            byte[] bytes = reader.GetBlobBytes(info.Value);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }

    return null;
}
