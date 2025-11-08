using DotnetRelease.Cves;
using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// Provides deep navigation into a specific year of .NET release history with CVE information.
/// Data is fetched lazily and cached automatically by ReleaseNotesGraph.
/// </summary>
public class ArchiveNavigator
{
    private readonly ReleaseNotesGraph _graph;
    private readonly string _year;

    public ArchiveNavigator(ReleaseNotesGraph graph, string year)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(year);
        _graph = graph;
        _year = year;
    }

    /// <summary>
    /// The year being navigated (e.g., "2024")
    /// </summary>
    public string Year => _year;

    /// <summary>
    /// Gets the raw year index document.
    /// </summary>
    public async Task<HistoryYearIndex> GetYearIndexAsync(CancellationToken cancellationToken = default)
    {
        return await _graph.GetYearIndexAsync(_year, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to load year index for {_year}");
    }

    /// <summary>
    /// Gets all months in this year (latest first).
    /// </summary>
    public async Task<IEnumerable<MonthSummary>> GetAllMonthsAsync(CancellationToken cancellationToken = default)
    {
        var index = await GetYearIndexAsync(cancellationToken);
        return index.Embedded?.Months?.Select(m => new MonthSummary(m, _year))
            ?? Enumerable.Empty<MonthSummary>();
    }

    /// <summary>
    /// Gets a specific month by month string (e.g., "01", "12").
    /// Returns null if the month is not found.
    /// </summary>
    public async Task<MonthSummary?> GetMonthAsync(string month, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(month);

        var months = await GetAllMonthsAsync(cancellationToken);
        return months.FirstOrDefault(m => m.Month == month);
    }

    /// <summary>
    /// Gets all CVE summaries for this year (from embedded data).
    /// </summary>
    public async Task<IEnumerable<CveRecordSummary>> GetCveSummariesAsync(CancellationToken cancellationToken = default)
    {
        var months = await GetAllMonthsAsync(cancellationToken);
        return months
            .Where(m => m.CveRecords is not null)
            .SelectMany(m => m.CveRecords!);
    }

    /// <summary>
    /// Gets all CVE IDs for this year (simple string list).
    /// </summary>
    public async Task<IEnumerable<string>> GetCveIdsAsync(CancellationToken cancellationToken = default)
    {
        var summaries = await GetCveSummariesAsync(cancellationToken);
        return summaries.Select(c => c.Id);
    }

    /// <summary>
    /// Gets the count of CVEs for this year.
    /// </summary>
    public async Task<int> GetCveCountAsync(CancellationToken cancellationToken = default)
    {
        var summaries = await GetCveSummariesAsync(cancellationToken);
        return summaries.Count();
    }

    /// <summary>
    /// Gets only the months that have CVE records.
    /// </summary>
    public async Task<IEnumerable<MonthSummary>> GetMonthsWithCvesAsync(CancellationToken cancellationToken = default)
    {
        var months = await GetAllMonthsAsync(cancellationToken);
        return months.Where(m => m.HasCves);
    }

    /// <summary>
    /// Gets the full CveRecords document for a specific month.
    /// This fetches the detailed cve.json file with all CVE information.
    /// Returns null if the month has no CVE data.
    /// </summary>
    public async Task<CveRecords?> GetCveRecordsForMonthAsync(string month, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(month);

        var monthIndex = await _graph.GetMonthIndexAsync(_year, month, cancellationToken);
        if (monthIndex?.Links is null || !monthIndex.Links.TryGetValue("cve-json", out var cveLink))
        {
            return null;
        }

        // Fetch the full CveRecords document
        return await _graph.FollowLinkAsync<CveRecords>(cveLink, cancellationToken);
    }
}
