namespace DotnetRelease.Graph;

/// <summary>
/// Standard titles and descriptions for .NET release indexes
/// Uses string interning to ensure single instances of commonly used strings
/// </summary>
public static class IndexTitles
{
    // Version Index (organized by version number)
    public static readonly string VersionIndexTitle = string.Intern(".NET Version Index");
    public static readonly string VersionIndexLink = string.Intern(".NET Version Index");
    
    // Timeline Index (organized chronologically)
    public static readonly string TimelineIndexTitle = string.Intern(".NET Release Timeline Index");
    public static readonly string TimelineIndexLink = string.Intern(".NET Release Timeline Index");
    
    // Year-level timeline
    public static string TimelineYearTitle(string year) => string.Intern($".NET Release Timeline Index - {year}");
    public static string TimelineYearDescription(string year) => string.Intern($".NET release timeline for {year}");
    public static string TimelineYearLink(string year) => string.Intern($"Release timeline index for {year}");
    
    // Month-level timeline
    public static string TimelineMonthTitle(string year, string month) => string.Intern($".NET Release Timeline Index - {year}-{month}");
    public static string TimelineMonthLink(string year, string month) => string.Intern($"Release timeline index for {year}-{month}");
    
    // Description patterns
    public static string VersionIndexDescription(string versionRange, string cacheNote) => 
        string.Intern($"Index of .NET versions {versionRange} (latest first); {cacheNote}");
    
    public static string TimelineIndexDescription(string versionRange, string cacheNote) => 
        string.Intern($"Timeline of .NET releases {versionRange} (latest first); {cacheNote}");
    
    public static string TimelineYearIndexDescription(string year, string versionRange, string cacheNote) =>
        string.Intern($"Release timeline for {year} ({versionRange}, latest first); {cacheNote}");
    
    public static string TimelineMonthIndexDescription(string year, string month, string versionRange, string cacheNote) =>
        string.Intern($"Release timeline for {year}-{month} ({versionRange}, latest first); {cacheNote}");
}
