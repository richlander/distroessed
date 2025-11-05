using DotnetRelease.Cves;
using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// Provides a high-level summary of .NET release history archives with CVE information.
/// Data is fetched lazily on first query and cached for subsequent queries.
/// </summary>
public class ArchivesSummary
{
    private readonly ReleaseNotesGraph _graph;
    private ReleaseHistoryIndex? _historyIndex;

    public ArchivesSummary(ReleaseNotesGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _graph = graph;
    }

    /// <summary>
    /// Ensures the release history index is loaded, fetching it if necessary.
    /// </summary>
    private async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        _historyIndex ??= await _graph.GetReleaseHistoryIndexAsync(cancellationToken)
            ?? throw new InvalidOperationException("Failed to load release history index");
    }

    /// <summary>
    /// Gets all years in the release history (latest first).
    /// </summary>
    public async Task<IEnumerable<YearSummary>> GetAllYearsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _historyIndex!.Embedded?.Years?.Select(y => new YearSummary(y))
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
    /// Gets all CVE summaries across all years.
    /// Note: This requires fetching each year's index to get month-level CVE data.
    /// </summary>
    public async Task<IEnumerable<CveRecordSummary>> GetAllCveSummariesAsync(CancellationToken cancellationToken = default)
    {
        var years = await GetAllYearsAsync(cancellationToken);
        var allCves = new List<CveRecordSummary>();

        foreach (var year in years)
        {
            var navigator = GetNavigator(year.Year);
            var yearCves = await navigator.GetCveSummariesAsync(cancellationToken);
            allCves.AddRange(yearCves);
        }

        return allCves;
    }

    /// <summary>
    /// Gets all CVE IDs across all years (simple string list).
    /// </summary>
    public async Task<IEnumerable<string>> GetAllCveIdsAsync(CancellationToken cancellationToken = default)
    {
        var summaries = await GetAllCveSummariesAsync(cancellationToken);
        return summaries.Select(c => c.Id);
    }

    /// <summary>
    /// Creates an ArchiveNavigator for deep exploration of a specific year.
    /// </summary>
    public ArchiveNavigator GetNavigator(string year)
    {
        return new ArchiveNavigator(_graph, year);
    }
}
