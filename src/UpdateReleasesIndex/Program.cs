using System.Text.Json;
using DotnetRelease;

const string majorReleaseFile = "releases.json";
const string patchReleaseFile = "release.json";
string baseDir = args.Length > 0 ? args[0] : "release-notes";

List<MajorReleaseIndexItem> majorReleases = [];

foreach (var versionDir in ReleaseNotes.GetReleaseNoteDirectories(new DirectoryInfo(baseDir)))
{
    string releasesJsonPath = Path.Combine(versionDir.FullName, majorReleaseFile);
    Stream releasesJson = File.OpenRead(releasesJsonPath);
    var major = await ReleaseNotes.GetMajorRelease(releasesJson) ?? throw new();
    
    // Use relative path from release-notes directory to match URL format
    string relativePath = Path.GetRelativePath(baseDir, versionDir.FullName);
    string releasesJsonUri = $"{ReleaseNotes.OfficialBaseUri}{relativePath}/{majorReleaseFile}";

    var majorReleaseItem = new MajorReleaseIndexItem(
        major.ChannelVersion,
        major.LatestRelease,
        major.LatestReleaseDate,
        major.LatestReleaseSecurity,
        major.LatestRuntime,
        major.LatestSdk,
        major.Product,
        major.SupportPhase,
        major.EolDate,
        major.ReleaseType,
        releasesJsonUri,
        releasesJsonUri,
        major.PatchReleasesIndexUri,
        major.SupportedOsInfoUri,
        major.SupportedOsInfoUri,
        major.OsPackagesInfoUri);

    majorReleases.Add(majorReleaseItem);

    if (major.SupportPhase is SupportPhase.Eol)
    {
        continue;
    }

    ProcessMajorRelease(major, versionDir.FullName, baseDir);
}

MajorReleasesIndex index = new(majorReleases);
string majorReleasesIndexJson = Path.Combine(baseDir, "releases-index.json");
WriteMajorReleasesIndex(index, majorReleasesIndexJson);

static void ProcessMajorRelease(MajorReleaseOverview majorReleaseOverview, string dir, string baseDir)
{
    List<PatchReleaseIndexItem> patchReleases = [];
    string channelVersion = majorReleaseOverview.ChannelVersion;
    int majorVersion = (int)decimal.Parse(channelVersion);

    foreach (var release in majorReleaseOverview.Releases)
    {
        var (name, folder, isPreview) = ReleaseNotes.GetReleaseName(release.ReleaseVersion);
        if (isPreview && majorVersion < 9)
        {
            continue;
        }
        
        // Use relative path from release-notes directory to match URL format
        string patchReleaseDir = Path.Combine(dir, folder);
        string relativePath = Path.GetRelativePath(baseDir, patchReleaseDir);
        var releaseJsonUrl = $"{ReleaseNotes.OfficialBaseUri}{relativePath}/{patchReleaseFile}";
        string patchReleaseJson = Path.Combine(patchReleaseDir, ReleaseNotes.PatchRelease);

        var patchReleaseOverview = new PatchReleaseOverview(
            channelVersion,
            release.ReleaseDate,
            release.ReleaseVersion,
            release.Security,
            release);

        var patchItem = new PatchReleaseIndexItem(
            release.ReleaseVersion,
            release.ReleaseDate,
            release.Security,
            releaseJsonUrl);

        patchReleases.Add(patchItem);

        WritePatchReleaseJson(patchReleaseOverview, patchReleaseJson);
    }

    var latestRelease = majorReleaseOverview.Releases[0];

    var index = new PatchReleasesIndex(
        channelVersion,
        latestRelease.ReleaseVersion,
        latestRelease.ReleaseDate,
        latestRelease.Security,
        majorReleaseOverview.SupportedOsInfoUri,
        majorReleaseOverview.OsPackagesInfoUri,
        patchReleases);

    string patchReleasesIndexJson = Path.Combine(dir, ReleaseNotes.PatchReleasesIndex);
    WritePatchReleasesIndex(index, patchReleasesIndexJson);
}

static void WriteMajorReleasesIndex(MajorReleasesIndex index, string file)
{
    var json = JsonSerializer.Serialize(index, MajorReleasesIndexSerializerContext.Default.MajorReleasesIndex);
    File.WriteAllText(file, json);
    Console.WriteLine($"Writing {file}");
}

static void WritePatchReleasesIndex(PatchReleasesIndex index, string file)
{
    var json = JsonSerializer.Serialize(index, PatchReleasesIndexSerializerContext.Default.PatchReleasesIndex);
    File.WriteAllText(file, json);
    Console.WriteLine($"Writing {file}");
}

static void WritePatchReleaseJson(PatchReleaseOverview release, string file)
{
    var json = JsonSerializer.Serialize(release, PatchReleaseOverviewSerializerContext.Default.PatchReleaseOverview);    
    File.WriteAllText(file, json);
    Console.WriteLine($"Writing {file}");
}
