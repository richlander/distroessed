namespace DotnetRelease.Graph;

/// <summary>
/// Standard link relation names for .NET release index files.
/// These constants ensure consistency between "kind" values and link relation names.
/// Pattern: {kind}-index where kind is the differentiator (release, major, patch, timeline, year, month)
/// </summary>
public static class LinkRelations
{
    // Version-based hierarchy (organized by version number)
    // Root → Major → Patch
    
    /// <summary>
    /// Link relation for root releases index (index.json)
    /// Points to documents with kind="releases-index"
    /// </summary>
    public const string ReleasesIndex = "releases-index";
    
    /// <summary>
    /// Link relation for major version index (e.g., 8.0/index.json)
    /// Points to documents with kind="major-version-index"
    /// </summary>
    public const string ReleaseMajor = "release-major";
    
    /// <summary>
    /// Link relation for patch version index (e.g., 8.0.1/index.json)
    /// Points to documents with kind="patch-version-index"
    /// </summary>
    public const string PatchVersionIndex = "patch-version-index";
    
    // Timeline-based hierarchy (organized chronologically)
    // Timeline → Year → Month
    
    /// <summary>
    /// Link relation for root timeline index (timeline/index.json)
    /// Points to documents with kind="timeline-index"
    /// </summary>
    public const string TimelineIndex = "timeline-index";
    
    /// <summary>
    /// Link relation for year timeline index (e.g., timeline/2024/index.json)
    /// Points to documents with kind="year-index"
    /// </summary>
    public const string YearIndex = "year-index";
    
    /// <summary>
    /// Link relation for month timeline index (e.g., timeline/2024/11/index.json)
    /// Points to documents with kind="month-index"
    /// </summary>
    public const string MonthIndex = "month-index";
    
    // Manifest and supplementary documents
    
    /// <summary>
    /// Link relation for release manifest documents
    /// </summary>
    public const string ReleaseManifest = "release-manifest";
    
    /// <summary>
    /// Link relation for CVE information documents
    /// </summary>
    public const string CveJson = "cve-json";
    
    /// <summary>
    /// Link relation for release information documents
    /// </summary>
    public const string Release = "release";
    
    // Latest link relations
    
    /// <summary>
    /// Link relation for latest item in current collection.
    /// Context-dependent: latest major (releases-index), latest patch (major-version-index), 
    /// latest SDK band (sdk-index). The containing document's kind provides the noun.
    /// </summary>
    public const string Latest = "latest";
    
    /// <summary>
    /// Link relation for latest LTS (Long-Term Support) version (releases-index only)
    /// Points to the most recent Long-Term Support major version
    /// </summary>
    public const string LatestLts = "latest-lts";
    
    /// <summary>
    /// Link relation for latest SDK index.
    /// Points to the SDK index (e.g., 8.0/sdk/index.json) for a major version.
    /// </summary>
    public const string LatestSdk = "latest-sdk";
    
    /// <summary>
    /// Link relation for latest patch with security fixes (major-version-index only)
    /// Points to the most recent patch that includes CVE fixes
    /// </summary>
    public const string LatestSecurity = "latest-security";
    
    /// <summary>
    /// Link relation for latest year (timeline-index only)
    /// Points to the most recent year index in the timeline.
    /// Note: Due to CDN caching, year-index should not have latest-month
    /// to avoid cache inconsistency issues
    /// </summary>
    public const string LatestYear = "latest-year";
    
    /// <summary>
    /// Link relation for latest month (year-index only)
    /// Points to the most recent month index within the year.
    /// Cache-safe: year-index controls its own months (no multi-level chain).
    /// </summary>
    public const string LatestMonth = "latest-month";

    /// <summary>
    /// Link relation for latest month with security releases (year-index only)
    /// Points to the most recent month index within the year that had security patches.
    /// </summary>
    public const string LatestSecurityMonth = "latest-security-month";

    /// <summary>
    /// Link relation for latest release within a year (year-index only)
    /// Points to the major version index for the highest .NET version released in the year.
    /// </summary>
    public const string LatestRelease = "latest-release";

    /// <summary>
    /// Link relation for latest patch of a major version.
    /// Used in embedded release entries to point directly to the latest patch index.
    /// </summary>
    public const string LatestPatch = "latest-patch";

    /// <summary>
    /// Link relation for the month a patch was released in.
    /// Used in patch-version-index to link to the timeline month index.
    /// </summary>
    public const string ReleaseMonth = "release-month";

    /// <summary>
    /// Link relation for the year a patch was released in.
    /// Used in patch-version-index to link to the timeline year index.
    /// </summary>
    public const string ReleaseYear = "release-year";

    /// <summary>
    /// Link relation for breaking changes document.
    /// Used in major-version-index to link to breaking-changes.json.
    /// </summary>
    public const string BreakingChangesJson = "breaking-changes-json";
}
