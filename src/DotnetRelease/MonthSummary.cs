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
    /// True if this month had security releases
    /// </summary>
    public bool Security => _monthSummary.Security;

    /// <summary>
    /// HAL links for navigation to this month's content
    /// </summary>
    public IReadOnlyDictionary<string, HalLink>? Links => _monthSummary.Links;
}
