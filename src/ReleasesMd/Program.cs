using System.Text;
using DotnetRelease;
using DotnetRelease.Index;
using DotnetRelease.ReleaseInfo;
using FileHelpers;
using MarkdownHelpers;

// Invocation forms:
// ReleasesMd generate ~/git/core/releases.md
// ReleasesMd generate ~/git/core/releases.md ~/git/core/release-notes

const string generate = "generate";

if (args.Length == 0)
{
    ShowHelp();
    return;
}

string verb = args[0].ToLowerInvariant();

if (verb != generate)
{
    Console.WriteLine($"Unknown verb: {verb}");
    ShowHelp();
    return;
}

if (args.Length < 2)
{
    Console.WriteLine("Error: Path argument required for 'generate' command.");
    ShowHelp();
    return;
}

string targetPath = args[1];
string basePath = args.Length > 2 ? args[2] : ReleaseNotes.GitHubBaseUri;

await GenerateReleasesMd(targetPath, basePath);

static void ShowHelp()
{
    Console.WriteLine("ReleasesMd - Generate releases.md from release-notes data");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  ReleasesMd generate <output-path> [source-path]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  output-path  - Path where releases.md will be written");
    Console.WriteLine("  source-path  - Optional path or URL to release-notes directory");
    Console.WriteLine("                 (default: GitHub release-index)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  ReleasesMd generate ~/git/core/releases.md");
    Console.WriteLine("  ReleasesMd generate releases.md ~/git/core/release-notes");
}

static async Task GenerateReleasesMd(string targetPath, string basePath)
{
    // Get path adaptor
    using HttpClient client = new();
    IAdaptivePath path = AdaptivePath.GetFromDefaultAdaptors(basePath, client);

    // Acquire JSON data
    string releaseIndexJson = path.Combine(ReleaseNotes.MajorReleasesIndex);
    using Stream releaseIndexStream = await path.GetStreamAsync(releaseIndexJson);
    MajorReleasesIndex index = await ReleaseNotes.GetMajorReleasesIndex(releaseIndexStream) ?? throw new Exception("Failed to load releases index");

    // Load lifecycle data and blog links for each release
    var releasesWithLifecycle = new List<ReleaseWithLifecycle>();
    foreach (var release in index.ReleasesIndex)
    {
        var (releaseDate, releaseBlog, eolBlog) = await LoadManifestData(path, release.ChannelVersion);
        releasesWithLifecycle.Add(new ReleaseWithLifecycle(release, releaseDate, releaseBlog, eolBlog));
    }

    // Separate supported and EOL releases
    var supportedReleases = releasesWithLifecycle
        .Where(r => r.Release.SupportPhase != SupportPhase.Eol)
        .OrderByDescending(r => ParseVersion(r.Release.ChannelVersion))
        .ToList();

    var eolReleases = releasesWithLifecycle
        .Where(r => r.Release.SupportPhase == SupportPhase.Eol)
        .OrderByDescending(r => ParseVersion(r.Release.ChannelVersion))
        .ToList();

    // Open target file
    using FileStream targetStream = File.Open(targetPath, FileMode.Create);
    using StreamWriter writer = new(targetStream);

    // Write header
    writer.WriteLine("# .NET Releases");
    writer.WriteLine();
    writer.WriteLine("The .NET team releases new major versions of .NET annually, each November. Releases are either Long Term Support (LTS) or Standard Term Support (STS), and transition from full support through to end-of-life on a defined schedule, per [.NET release policies][policies]. .NET releases are [supported by Microsoft](microsoft-support.md) on [multiple operating systems](os-lifecycle-policy.md) and hardware architectures.");
    writer.WriteLine();
    writer.WriteLine("All .NET versions can be downloaded from the [.NET Website](https://dotnet.microsoft.com/download/dotnet), [Linux Package Managers](https://learn.microsoft.com/dotnet/core/install/linux), and the [Microsoft Artifact Registry](https://mcr.microsoft.com/catalog?search=dotnet/).");
    writer.WriteLine();

    // Write supported releases section
    writer.WriteLine("## Supported releases");
    writer.WriteLine();
    writer.WriteLine("The following table lists supported releases.");
    writer.WriteLine();

    WriteSupportedReleasesTable(writer, supportedReleases);

    // Write EOL releases section
    writer.WriteLine();
    writer.WriteLine("## End-of-life releases");
    writer.WriteLine();
    writer.WriteLine("The following table lists end-of-life releases.");
    writer.WriteLine();

    WriteEolReleasesTable(writer, eolReleases);

    // Write footer
    writer.WriteLine();
    writer.WriteLine("[policies]: release-policies.md");

    writer.Close();
    targetStream.Close();

    var writtenFile = new FileInfo(targetPath);
    Console.WriteLine($"Generated {writtenFile.Length} bytes");
    Console.WriteLine(writtenFile.FullName);
}

static async Task<(DateTime?, string?, string?)> LoadManifestData(IAdaptivePath path, string channelVersion)
{
    try
    {
        string manifestPath = path.Combine(channelVersion, "manifest.json");
        using Stream manifestStream = await path.GetStreamAsync(manifestPath);
        using StreamReader reader = new(manifestStream);
        string json = await reader.ReadToEndAsync();
        
        // Parse manifest data manually to avoid full deserialization
        var doc = System.Text.Json.JsonDocument.Parse(json);
        
        DateTime? releaseDate = null;
        if (doc.RootElement.TryGetProperty("lifecycle", out var lifecycle))
        {
            if (lifecycle.TryGetProperty("release-date", out var releaseDateProp))
            {
                releaseDate = releaseDateProp.GetDateTime();
            }
        }
        
        string? releaseBlog = null;
        string? eolBlog = null;
        if (doc.RootElement.TryGetProperty("_links", out var links))
        {
            if (links.TryGetProperty("release-blog", out var releaseBlogProp))
            {
                if (releaseBlogProp.TryGetProperty("href", out var hrefProp))
                {
                    releaseBlog = hrefProp.GetString();
                }
            }
            if (links.TryGetProperty("eol-blog", out var eolBlogProp))
            {
                if (eolBlogProp.TryGetProperty("href", out var eolHrefProp))
                {
                    eolBlog = eolHrefProp.GetString();
                }
            }
        }
        
        return (releaseDate, releaseBlog, eolBlog);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not load manifest data for {channelVersion}: {ex.Message}");
    }
    return (null, null, null);
}

static double ParseVersion(string version)
{
    if (double.TryParse(version, out double result))
        return result;
    return 0;
}

static void WriteSupportedReleasesTable(StreamWriter writer, List<ReleaseWithLifecycle> releases)
{
    Table table = new();
    Link links = new();

    table.AddHeader("Version", "Release Date", "Release type", "Support phase", "Latest Patch Version", "End of Support");

    foreach (var item in releases)
    {
        var release = item.Release;
        string version = FormatVersion(release.ChannelVersion);
        string versionLink = links.AddReferenceLink(version, GetReleaseNotesPath(release.ChannelVersion));
        
        string releaseDate = FormatReleaseDate(release, item.InitialReleaseDate, item.ReleaseBlogUrl);
        string releaseType = FormatReleaseType(links, release.ReleaseType);
        string supportPhase = FormatSupportPhase(release.SupportPhase);
        
        string latestPatch = FormatLatestPatch(release);
        string latestPatchLink = links.AddReferenceLink(latestPatch, GetPatchNotesPath(release.ChannelVersion, release.LatestRelease));
        
        string eolDate = FormatEolDate(release.EolDate);

        table.AddRow(versionLink, releaseDate, releaseType, supportPhase, latestPatchLink, eolDate);
    }

    writer.Write(table);
    writer.WriteLine();

    foreach (string link in links.GetReferenceLinkAnchors())
    {
        writer.WriteLine(link);
    }
}

static void WriteEolReleasesTable(StreamWriter writer, List<ReleaseWithLifecycle> releases)
{
    Table table = new();
    Link links = new();

    table.AddHeader("Version", "Release Date", "Support", "Final Patch Version", "End of Support");

    foreach (var item in releases)
    {
        var release = item.Release;
        string version = FormatVersionEol(release.ChannelVersion);
        string versionLink = links.AddReferenceLink(version, GetReleaseNotesPath(release.ChannelVersion));
        
        string releaseDate = FormatReleaseDate(release, item.InitialReleaseDate, item.ReleaseBlogUrl);
        string releaseType = FormatReleaseType(links, release.ReleaseType);
        
        string latestPatch = FormatLatestPatch(release);
        string latestPatchLink = links.AddReferenceLink(latestPatch, GetPatchNotesPath(release.ChannelVersion, release.LatestRelease));
        
        string eolDate = FormatEolDateWithLink(release, item.EolBlogUrl);

        table.AddRow(versionLink, releaseDate, releaseType, latestPatchLink, eolDate);
    }

    writer.Write(table);
    writer.WriteLine();

    foreach (string link in links.GetReferenceLinkAnchors())
    {
        writer.WriteLine(link);
    }
}

static string FormatVersion(string channelVersion)
{
    return $".NET {channelVersion}";
}

static string FormatVersionEol(string channelVersion)
{
    // For EOL releases, use historical naming
    if (channelVersion.StartsWith("1.") || channelVersion.StartsWith("2.") || channelVersion.StartsWith("3."))
    {
        return $".NET Core {channelVersion}";
    }
    return $".NET {channelVersion}";
}

static string FormatReleaseDate(MajorReleaseIndexItem release, DateTime? initialReleaseDate, string? releaseBlogUrl)
{
    // Use initial release date if available, otherwise fall back to latest release date
    DateOnly dateToUse = initialReleaseDate.HasValue 
        ? DateOnly.FromDateTime(initialReleaseDate.Value)
        : release.LatestReleaseDate;
    
    string date = dateToUse.ToString("MMMM d, yyyy");
    
    // Use blog URL from manifest if available
    if (releaseBlogUrl != null)
    {
        return $"[{date}]({releaseBlogUrl})";
    }
    return date;
}

static string FormatReleaseType(Link links, ReleaseType releaseType)
{
    string typeStr = releaseType.ToString().ToUpper();
    return links.AddReferenceLink(typeStr, "policies");
}

static string FormatSupportPhase(SupportPhase phase)
{
    return phase switch
    {
        SupportPhase.Preview => "Preview",
        SupportPhase.GoLive => "Go-Live",
        SupportPhase.Active => "Active",
        SupportPhase.Maintenance => "Maintenance",
        SupportPhase.Eol => "End of Life",
        _ => phase.ToString()
    };
}

static string FormatLatestPatch(MajorReleaseIndexItem release)
{
    return release.LatestRelease;
}

static string FormatEolDate(DateOnly eolDate)
{
    return eolDate.ToString("MMMM d, yyyy");
}

static string FormatEolDateWithLink(MajorReleaseIndexItem release, string? eolBlogUrl)
{
    string date = release.EolDate.ToString("MMMM d, yyyy");
    
    // Use blog URL from manifest if available
    if (eolBlogUrl != null)
    {
        return $"[{date}]({eolBlogUrl})";
    }
    return date;
}

static string GetReleaseNotesPath(string channelVersion)
{
    return $"release-notes/{channelVersion}/README.md";
}

static string GetPatchNotesPath(string channelVersion, string patchVersion)
{
    return $"release-notes/{channelVersion}/{patchVersion}/{patchVersion}.md";
}

record ReleaseWithLifecycle(MajorReleaseIndexItem Release, DateTime? InitialReleaseDate, string? ReleaseBlogUrl, string? EolBlogUrl);
