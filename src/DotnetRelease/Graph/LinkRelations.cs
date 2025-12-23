namespace DotnetRelease.Graph;

/// <summary>
/// Standard link relation names for .NET release index files.
/// Link relations use simple nouns matching kind values.
/// </summary>
public static class LinkRelations
{
    // Version-based hierarchy (organized by version number)
    // Root → Major → Patch

    /// <summary>
    /// Link relation for root releases index (index.json)
    /// Points to documents with kind="root"
    /// </summary>
    public const string Root = "root";

    /// <summary>
    /// Link relation for major version index (e.g., 9.0/index.json)
    /// Points to documents with kind="major"
    /// </summary>
    public const string Major = "major";

    /// <summary>
    /// Link relation for patch version index (e.g., 9.0/9.0.1/index.json)
    /// Points to documents with kind="patch"
    /// </summary>
    public const string Patch = "patch";

    // Timeline-based hierarchy (organized chronologically)
    // Timeline → Year → Month

    /// <summary>
    /// Link relation for root timeline index (timeline/index.json)
    /// Points to documents with kind="timeline"
    /// </summary>
    public const string Timeline = "timeline";

    /// <summary>
    /// Link relation for year timeline index (e.g., timeline/2024/index.json)
    /// Points to documents with kind="year"
    /// </summary>
    public const string Year = "year";

    /// <summary>
    /// Link relation for month timeline index (e.g., timeline/2024/11/index.json)
    /// Points to documents with kind="month"
    /// </summary>
    public const string Month = "month";

    // Manifest and supplementary documents

    /// <summary>
    /// Link relation for manifest documents (context-dependent)
    /// </summary>
    public const string Manifest = "manifest";

    /// <summary>
    /// Link relation for major version manifest when referenced from a patch context.
    /// Disambiguates from patch-level manifests.
    /// </summary>
    public const string MajorManifest = "major-manifest";

    /// <summary>
    /// Link relation for CVE information documents
    /// </summary>
    public const string CveJson = "cve-json";

    /// <summary>
    /// Link relation for release information documents
    /// </summary>
    public const string Release = "release";

    // Latest link relations (fully qualified)

    /// <summary>
    /// Link relation for latest major version.
    /// Used in root and timeline to point to the latest stable major version.
    /// </summary>
    public const string LatestMajor = "latest-major";

    /// <summary>
    /// Link relation for latest LTS (Long-Term Support) major version.
    /// Used in root and timeline to point to the latest LTS major version.
    /// </summary>
    public const string LatestLtsMajor = "latest-lts-major";

    /// <summary>
    /// Link relation for downloads index.
    /// Points to the downloads index (e.g., 9.0/downloads/index.json) for a major version.
    /// </summary>
    public const string Downloads = "downloads";

    /// <summary>
    /// Link relation for latest patch with security fixes.
    /// Used in major to point to the most recent patch that includes CVE fixes.
    /// </summary>
    public const string LatestSecurityPatch = "latest-security-patch";

    /// <summary>
    /// Link relation for latest year (timeline only)
    /// Points to the most recent year index in the timeline.
    /// </summary>
    public const string LatestYear = "latest-year";

    /// <summary>
    /// Link relation for latest month (year only)
    /// Points to the most recent month index within the year.
    /// </summary>
    public const string LatestMonth = "latest-month";

    /// <summary>
    /// Link relation for latest month with security releases (year only)
    /// Points to the most recent month index within the year that had security patches.
    /// </summary>
    public const string LatestSecurityMonth = "latest-security-month";

    /// <summary>
    /// Link relation for latest CVE JSON file.
    /// Points to the cve.json in the most recent month with security releases.
    /// </summary>
    public const string LatestCveJson = "latest-cve-json";

    /// <summary>
    /// Link relation for latest patch of a major version.
    /// Used in embedded release entries to point directly to the latest patch index.
    /// </summary>
    public const string LatestPatch = "latest-patch";

    /// <summary>
    /// Link relation for compatibility document.
    /// Used in major to link to compatibility.json.
    /// </summary>
    public const string CompatibilityJson = "compatibility-json";

    /// <summary>
    /// Link relation for target frameworks document.
    /// Used in major to link to target-frameworks.json.
    /// </summary>
    public const string TargetFrameworksJson = "target-frameworks-json";

    // Previous link relations (fully qualified)

    /// <summary>
    /// Link relation for previous patch release.
    /// Used in patch to navigate to the previous patch in the same major version.
    /// </summary>
    public const string PrevPatch = "prev-patch";

    /// <summary>
    /// Link relation for previous month.
    /// Used in month to navigate to the previous month in the timeline.
    /// </summary>
    public const string PrevMonth = "prev-month";

    /// <summary>
    /// Link relation for previous year.
    /// Used in year to navigate to the previous year in the timeline.
    /// </summary>
    public const string PrevYear = "prev-year";

    /// <summary>
    /// Link relation for previous security patch release.
    /// Used in patch to navigate to the previous patch with security fixes.
    /// Navigation pattern: start from "latest-security-patch" and walk via "prev-security-patch" links.
    /// </summary>
    public const string PrevSecurityPatch = "prev-security-patch";

    /// <summary>
    /// Link relation for previous security month.
    /// Used in month to navigate to the previous month with security fixes.
    /// Navigation pattern: start from "latest-security-month" and walk via "prev-security-month" links.
    /// </summary>
    public const string PrevSecurityMonth = "prev-security-month";
}
