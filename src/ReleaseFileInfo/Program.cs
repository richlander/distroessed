using DotnetRelease;

const string releasesUrl = "https://raw.githubusercontent.com/dotnet/core/refs/heads/main/release-notes/9.0/releases.json";

if (args.Length == 0)
{
    Console.WriteLine("Usage: ReleaseFileInfo <file-type> [count]");
    Console.WriteLine("Example: ReleaseFileInfo dotnet-sdk-win-x64.zip 5");
    Console.WriteLine("         ReleaseFileInfo dotnet-sdk-win-x64.zip    (shows all)");
    return 1;
}

string fileType = args[0];
int? maxCount = args.Length > 1 && int.TryParse(args[1], out var count) ? count : null;

try
{
    using var httpClient = new HttpClient();
    var stream = await httpClient.GetStreamAsync(releasesUrl);
    var majorRelease = await ReleaseNotes.GetMajorRelease(stream);

    if (majorRelease is null)
    {
        Console.WriteLine("Failed to download or parse releases.json");
        return 1;
    }

    int foundCount = 0;
    long? previousSize = null;
    
    foreach (var release in majorRelease.Releases)
    {
        if (maxCount.HasValue && foundCount >= maxCount.Value)
        {
            break;
        }

        var file = release.Sdk.Files.FirstOrDefault(f => f.Name == fileType);
        if (file is not null)
        {
            if (foundCount > 0)
            {
                Console.WriteLine();
            }

            Console.WriteLine($"Version: {release.Sdk.Version}");
            Console.WriteLine($"URL: {file.Url}");
            
            // Get file size from HTTP HEAD request
            var request = new HttpRequestMessage(HttpMethod.Head, file.Url);
            var response = await httpClient.SendAsync(request);
            if (response.Content.Headers.ContentLength.HasValue)
            {
                long currentSize = response.Content.Headers.ContentLength.Value;
                Console.WriteLine($"Size: {currentSize:N0} bytes");
                
                if (previousSize.HasValue)
                {
                    long change = currentSize - previousSize.Value;
                    string changeSign = change >= 0 ? "+" : "";
                    double changePercent = (double)change / previousSize.Value * 100;
                    Console.WriteLine($"Change: {changeSign}{change:N0} bytes ({changeSign}{changePercent:F2}%)");
                }
                
                previousSize = currentSize;
            }
            else
            {
                Console.WriteLine("Size: Unknown");
                previousSize = null;
            }
            
            foundCount++;
        }
    }

    if (foundCount == 0)
    {
        Console.WriteLine($"File type '{fileType}' not found in releases.");
        return 1;
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}
