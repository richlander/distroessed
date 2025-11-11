using DotnetRelease.Security;
using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// Provides a high-level summary of .NET release history archives with CVE information.
/// Data is fetched lazily and cached automatically by ReleaseNotesGraph.
/// </summary>
public class ArchivesSummary
{
    private readonly ReleaseNotesGraph _graph;

    public ArchivesSummary(ReleaseNotesGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _graph = graph;
    }

    /// <summary>
    /// Gets all years in the release history (latest first).
    /// </summary>
    public async Task<IEnumerable<YearSummary>> GetAllYearsAsync(CancellationToken cancellationToken = default)
    {
        var historyIndex = await _graph.GetReleaseHistoryIndexAsync(cancellationToken)
            ?? throw new InvalidOperationException("Failed to load release history index");
        return historyIndex.Embedded?.Years?.Select(y => new YearSummary(y))
            ?? Enumerable.Empty<YearSummary>();
    }

    /// <summary>
    /// Gets a specific year by year string (e.g., "2024").
    /// Returns null if the year is not found.
    /// </summary>
    public async Task<YearSummary?> GetYearAsync(string year, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(year);

        var years = await GetAllYearsAsync(cancellationToken);
        return years.FirstOrDefault(y => y.Year == year);
    }

    /// <summary>
    /// Gets the most recent year in the archive.
    /// </summary>
    public async Task<YearSummary?> GetLatestYearAsync(CancellationToken cancellationToken = default)
    {
        var years = await GetAllYearsAsync(cancellationToken);
        return years.FirstOrDefault();
    }

    /// <summary>
    /// Gets all CVE IDs across all years (year-level data only).
    /// For full CVE summaries, fetch month-level indices directly.
    /// </summary>
    public async Task<IEnumerable<string>> GetAllCveIdsAsync(CancellationToken cancellationToken = default)
    {
        var years = await GetAllYearsAsync(cancellationToken);
        var allCveIds = new List<string>();

        foreach (var year in years)
        {
            var navigator = GetNavigator(year.Year);
            var yearCveIds = await navigator.GetCveIdsAsync(cancellationToken);
            allCveIds.AddRange(yearCveIds);
        }

        return allCveIds.Distinct();
    }

    /// <summary>
    /// Gets all CVE summaries across all years.
    /// </summary>
    [Obsolete("CVE summaries are now at month level. Use GetAllCveIdsAsync() for CVE IDs or fetch month indices directly.")]
    public async Task<IEnumerable<CveRecordSummary>> GetAllCveSummariesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("CVE summaries are now only available at the month level. Use GetAllCveIdsAsync() for CVE IDs.");
    }

    /// <summary>
    /// Gets CVE IDs for a specific date range (year-level data only).
    /// For full CVE summaries, fetch month-level indices directly.
    /// </summary>
    /// <param name="startYear">Start year (inclusive)</param>
    /// <param name="startMonth">Start month (inclusive, 1-12)</param>
    /// <param name="endYear">End year (inclusive)</param>
    /// <param name="endMonth">End month (inclusive, 1-12)</param>
    public async Task<IEnumerable<string>> GetCveIdsInDateRangeAsync(
        int startYear, int startMonth, int endYear, int endMonth,
        CancellationToken cancellationToken = default)
    {
        var allCveIds = new List<string>();

        for (int year = startYear; year <= endYear; year++)
        {
            var navigator = GetNavigator(year.ToString());
            var months = await navigator.GetAllMonthsAsync(cancellationToken);

            foreach (var month in months)
            {
                var monthNum = int.Parse(month.Month);

                // Check if month is in range
                if (year == startYear && monthNum < startMonth) continue;
                if (year == endYear && monthNum > endMonth) continue;

                if (month.CveRecords is not null)
                {
                    allCveIds.AddRange(month.CveRecords);
                }
            }
        }

        return allCveIds;
    }

    /// <summary>
    /// Gets CVE summaries for a specific date range.
    /// </summary>
    [Obsolete("CVE summaries are now at month level. Use GetCveIdsInDateRangeAsync() for CVE IDs or fetch month indices directly.")]
    public async Task<IEnumerable<CveRecordSummary>> GetCvesInDateRangeAsync(
        int startYear, int startMonth, int endYear, int endMonth,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("CVE summaries are now only available at the month level. Use GetCveIdsInDateRangeAsync() for CVE IDs.");
    }

    /// <summary>
    /// Gets CVE IDs from the last N months.
    /// </summary>
    public async Task<IEnumerable<string>> GetRecentCveIdsAsync(
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var start = now.AddMonths(-monthsBack);

        return await GetCveIdsInDateRangeAsync(
            start.Year, start.Month,
            now.Year, now.Month,
            cancellationToken);
    }

    /// <summary>
    /// Gets CVEs from the last N months.
    /// </summary>
    [Obsolete("CVE summaries are now at month level. Use GetRecentCveIdsAsync() for CVE IDs or fetch month indices directly.")]
    public async Task<IEnumerable<CveRecordSummary>> GetRecentCvesAsync(
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("CVE summaries are now only available at the month level. Use GetRecentCveIdsAsync() for CVE IDs.");
    }

    /// <summary>
    /// Gets full CVE records for a date range.
    /// This fetches the detailed cve.json files for each month in the range.
    /// </summary>
    public async Task<IEnumerable<CveRecords>> GetCveRecordsInDateRangeAsync(
        int startYear, int startMonth, int endYear, int endMonth,
        CancellationToken cancellationToken = default)
    {
        var allRecords = new List<CveRecords>();

        for (int year = startYear; year <= endYear; year++)
        {
            var navigator = GetNavigator(year.ToString());
            var months = await navigator.GetAllMonthsAsync(cancellationToken);

            foreach (var month in months.Where(m => m.HasCves))
            {
                var monthNum = int.Parse(month.Month);

                // Check if month is in range
                if (year == startYear && monthNum < startMonth) continue;
                if (year == endYear && monthNum > endMonth) continue;

                var records = await navigator.GetCveRecordsForMonthAsync(month.Month, cancellationToken);
                if (records is not null)
                {
                    allRecords.Add(records);
                }
            }
        }

        return allRecords;
    }

    /// <summary>
    /// Gets full CVE records from the last N months.
    /// </summary>
    public async Task<IEnumerable<CveRecords>> GetRecentCveRecordsAsync(
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var start = now.AddMonths(-monthsBack);

        return await GetCveRecordsInDateRangeAsync(
            start.Year, start.Month,
            now.Year, now.Month,
            cancellationToken);
    }

    /// <summary>
    /// Creates an ArchiveNavigator for deep exploration of a specific year.
    /// </summary>
    public ArchiveNavigator GetNavigator(string year)
    {
        return new ArchiveNavigator(_graph, year);
    }
}
