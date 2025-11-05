using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// Provides a summary of .NET release history for a specific year.
/// This wraps data from HistoryYearEntry.
/// </summary>
public class YearSummary
{
    private readonly HistoryYearEntry _yearEntry;

    public YearSummary(HistoryYearEntry yearEntry)
    {
        ArgumentNullException.ThrowIfNull(yearEntry);
        _yearEntry = yearEntry;
    }

    /// <summary>
    /// The year (e.g., "2024", "2025")
    /// </summary>
    public string Year => _yearEntry.Year;

    /// <summary>
    /// Description of release activity for this year
    /// </summary>
    public string Description => _yearEntry.Description;

    /// <summary>
    /// .NET versions that had releases this year
    /// </summary>
    public IList<string>? DotnetReleases => _yearEntry.DotnetReleases;

    /// <summary>
    /// Number of .NET versions with releases this year
    /// </summary>
    public int ReleaseCount => DotnetReleases?.Count ?? 0;

    /// <summary>
    /// HAL links for navigation to this year's content
    /// </summary>
    public IReadOnlyDictionary<string, HalLink> Links => _yearEntry.Links;
}
