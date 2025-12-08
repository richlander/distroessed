namespace DotnetRelease;

/// <summary>
/// Centralized file name constants for .NET release data and schema files.
/// Use these constants instead of hardcoded strings to ensure consistency.
/// </summary>
public static class FileNames
{
    // Data file names
    public const string Index = "index.json";
    public const string Releases = "releases.json";
    public const string Release = "release.json";
    public const string Cve = "cve.json";
    public const string SupportedOs = "supported-os.json";
    public const string OsPackages = "os-packages.json";
    public const string Manifest = "manifest.json";
    public const string PartialManifest = "_manifest.json";
    public const string ReleasesIndex = "releases-index.json";
    public const string PatchReleasesIndex = "patch-releases-index.json";
    public const string Compatibility = "compatibility.json";
    public const string TargetFrameworks = "target-frameworks.json";

    // Schema file names (in schemas/ directory)
    public static class Schemas
    {
        public const string ReleasesIndex = "dotnet-releases-index.json";
        public const string Releases = "dotnet-releases.json";
        public const string PatchRelease = "dotnet-patch-release.json";
        public const string OsPackages = "dotnet-os-packages.json";
        public const string SupportedOs = "dotnet-supported-os.json";
        public const string Cves = "dotnet-cves.json";
        public const string ReleaseVersionIndex = "dotnet-release-version-index.json";
        public const string SdkVersionIndex = "dotnet-sdk-version-index.json";
        public const string SdkDownload = "dotnet-sdk-download.json";
        public const string PatchDetailIndex = "dotnet-patch-detail-index.json";
        public const string TimelineIndex = "dotnet-release-timeline-index.json";
        public const string DownloadsIndex = "dotnet-downloads-index.json";
        public const string ComponentDownload = "dotnet-component-download.json";
    }

    // Directory names
    public static class Directories
    {
        public const string Timeline = "timeline";
        public const string Schemas = "schemas";
        public const string Sdk = "sdk";
        public const string Preview = "preview";
        public const string Downloads = "downloads";
    }
}
