using System.Globalization;

namespace DotnetRelease.Graph;

/// <summary>
/// Standard titles and descriptions for .NET release indexes
/// Uses string interning to ensure single instances of commonly used strings
/// </summary>
public static class IndexTitles
{
    // Month name lookup for human-friendly date formatting
    private static readonly string[] MonthNames = CultureInfo.InvariantCulture.DateTimeFormat.MonthNames;

    /// <summary>
    /// Formats a year and month as "December 2024" style string
    /// </summary>
    public static string FormatMonthYear(string year, string month) =>
        int.TryParse(month, out var m) && m >= 1 && m <= 12
            ? $"{MonthNames[m - 1]} {year}"
            : $"{year}-{month}";

    // Version Index (organized by version number)
    public static readonly string VersionIndexTitle = string.Intern(".NET Release Index");
    public static readonly string VersionIndexLink = string.Intern(".NET Release Index");

    // Timeline Index (organized chronologically)
    public static readonly string TimelineIndexTitle = string.Intern(".NET Release Timeline Index");
    public static readonly string TimelineIndexLink = string.Intern(".NET Release Timeline Index");

    // Year-level timeline
    public static string TimelineYearTitle(string year) => string.Intern($".NET Year Timeline Index - {year}");
    public static string TimelineYearDescription(string year) => string.Intern($".NET release timeline - {year}");
    public static string TimelineYearLink(string year) => string.Intern($".NET Year Timeline Index - {year}");

    // Month-level timeline
    public static string TimelineMonthTitle(string year, string month) => string.Intern($".NET Month Timeline Index - {FormatMonthYear(year, month)}");
    public static string TimelineMonthLink(string year, string month) => string.Intern($".NET Month Timeline Index - {FormatMonthYear(year, month)}");

    // Description patterns
    public static string VersionIndexDescription(string latestVersion) =>
        string.Intern($".NET Release Index (latest: {latestVersion})");

    public static string TimelineIndexDescription(string latestVersion) =>
        string.Intern($".NET Release Timeline (latest: {latestVersion})");

    public static string TimelineYearIndexDescription(string year, string latestVersion) =>
        string.Intern($"Release timeline - {year} (latest: {latestVersion})");

    public static string TimelineMonthIndexDescription(string year, string month, string latestVersion) =>
        string.Intern($"Release timeline - {FormatMonthYear(year, month)} (latest: {latestVersion})");
}
