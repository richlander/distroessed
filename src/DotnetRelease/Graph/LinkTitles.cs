namespace DotnetRelease.Graph;

/// <summary>
/// Standard link titles for .NET release index files
/// Uses string interning to ensure single instances of commonly used strings.
/// Titles are kind-based (semantic) rather than instance-specific to reduce file size.
/// </summary>
public static class LinkTitles
{
    // Index titles (kind-based, not version-specific)
    public static readonly string Index = string.Intern("Index");
    public static readonly string ReleaseIndex = string.Intern("Release index");
    public static readonly string MajorVersionIndex = string.Intern("Major version index");
    public static readonly string PatchIndex = string.Intern("Patch index");
    public static readonly string SdkIndex = string.Intern("SDK index");
    public static readonly string DownloadsIndex = string.Intern("Downloads index");
    public static readonly string TimelineIndex = string.Intern("Timeline index");
    public static readonly string TimelineYearIndex = string.Intern("Timeline year index");
    public static readonly string TimelineMonthIndex = string.Intern("Release month index");

    // Legacy (for backward compatibility in some contexts)
    public static readonly string HistoryIndex = string.Intern("History Index");
    public static readonly string DotNetReleaseIndex = string.Intern(".NET Release Index");
    public static readonly string DotNetReleaseNotes = string.Intern(".NET Release Notes");
    public static readonly string ReleaseNotes = string.Intern("Release Notes");
    public static readonly string Release = string.Intern("Release");
    public static readonly string ReleaseManifest = string.Intern("Release manifest");
    public static readonly string CompleteReleaseInformation = string.Intern("Complete (large file) release information for all patch releases");

    // CVE-related
    public static readonly string CveRecordsJson = string.Intern("CVE records (JSON)");
    public static readonly string CveMarkdown = string.Intern("CVE information");

    // Latest pointers (kind-based)
    public static readonly string LatestPatch = string.Intern("Latest patch");
    public static readonly string LatestSecurityPatch = string.Intern("Latest security patch");
    public static readonly string LatestLts = string.Intern("Latest LTS");
    public static readonly string LatestSdk = string.Intern("Latest SDK");

    // Support and documentation
    public static readonly string SupportPolicy = string.Intern("Support Policy");
    public static readonly string UsageGuide = string.Intern("Usage Guide");
    public static readonly string QuickReference = string.Intern("Quick Reference");
    public static readonly string Glossary = string.Intern("Glossary");

    // OS and packages
    public static readonly string SupportedOSes = string.Intern("Supported OSes");
    public static readonly string OsPackages = string.Intern("OS Packages");
    public static readonly string LinuxPackages = string.Intern("Linux Packages");
}
