using DotnetRelease.Security;
using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// Provides a summary of .NET release history for a specific month.
/// This wraps data from HistoryMonthSummary.
/// </summary>
public class MonthSummary
{
    private readonly HistoryMonthSummary _monthSummary;
    private readonly string _year;

    public MonthSummary(HistoryMonthSummary monthSummary, string year)
    {
        ArgumentNullException.ThrowIfNull(monthSummary);
        ArgumentNullException.ThrowIfNull(year);
        _monthSummary = monthSummary;
        _year = year;
    }

    /// <summary>
    /// The year this month belongs to (e.g., "2024")
    /// </summary>
    public string Year => _year;

    /// <summary>
    /// The month (e.g., "01", "12")
    /// </summary>
    public string Month => _monthSummary.Month;

    /// <summary>
    /// Combined year-month for convenience (e.g., "2024-10")
    /// </summary>
    public string YearMonth => $"{_year}-{Month}";

    /// <summary>
    /// .NET versions that had releases this month
    /// </summary>
    public IList<string>? DotnetReleases => _monthSummary.DotnetReleases;

    /// <summary>
    /// CVE security vulnerability records for this month
    /// </summary>
    public IReadOnlyList<CveRecordSummary>? CveRecords => _monthSummary.CveRecords;

    /// <summary>
    /// True if this month has CVE records
    /// </summary>
    public bool HasCves => CveRecords?.Count > 0;

    /// <summary>
    /// Number of CVEs disclosed this month
    /// </summary>
    public int CveCount => CveRecords?.Count ?? 0;

    /// <summary>
    /// Gets just the CVE IDs for this month
    /// </summary>
    public IEnumerable<string> GetCveIds()
    {
        return CveRecords?.Select(c => c.Id) ?? Enumerable.Empty<string>();
    }

    /// <summary>
    /// HAL links for navigation to this month's content
    /// </summary>
    public IReadOnlyDictionary<string, HalLink>? Links => _monthSummary.Links;
}
