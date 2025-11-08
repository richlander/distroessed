using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// Provides programmatic access to the .NET release notes graph via HAL+JSON navigation.
/// Layer 2 implementation with automatic caching via ILinkFollower.
/// </summary>
public class ReleaseNotesGraph
{
    private readonly ILinkFollower _linkFollower;
    private readonly string _baseUrl;

    public ReleaseNotesGraph(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _linkFollower = new CachingLinkFollower(client);
        _baseUrl = ReleaseNotes.GitHubBaseUri;
    }

    public ReleaseNotesGraph(HttpClient client, string baseUrl) : this(client)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(baseUrl);
        _linkFollower = new CachingLinkFollower(client, baseUrl);
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Creates a ReleaseNotesGraph with a custom ILinkFollower implementation.
    /// Useful for testing or custom caching strategies.
    /// </summary>
    public ReleaseNotesGraph(ILinkFollower linkFollower, string baseUrl)
    {
        ArgumentNullException.ThrowIfNull(linkFollower);
        ArgumentNullException.ThrowIfNullOrEmpty(baseUrl);
        _linkFollower = linkFollower;
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Gets the root major release version index containing all .NET versions.
    /// URL: {baseUrl}/index.json
    /// Cached after first fetch.
    /// </summary>
    public async Task<MajorReleaseVersionIndex?> GetMajorReleaseIndexAsync(CancellationToken cancellationToken = default)
    {
        string url = $"{_baseUrl}index.json";
        return await _linkFollower.FetchAsync<MajorReleaseVersionIndex>(url, cancellationToken);
    }

    /// <summary>
    /// Gets the patch release index for a specific major version.
    /// URL: {baseUrl}/{version}/index.json
    /// Cached after first fetch.
    /// </summary>
    /// <param name="version">Major version (e.g., "8.0", "9.0")</param>
    public async Task<PatchReleaseVersionIndex?> GetPatchReleaseIndexAsync(string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(version);
        string url = $"{_baseUrl}{version}/index.json";
        return await _linkFollower.FetchAsync<PatchReleaseVersionIndex>(url, cancellationToken);
    }

    /// <summary>
    /// Gets the manifest for a specific major version.
    /// URL: {baseUrl}/{version}/manifest.json
    /// Cached after first fetch.
    /// </summary>
    /// <param name="version">Major version (e.g., "8.0", "9.0")</param>
    public async Task<ReleaseManifest?> GetManifestAsync(string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(version);
        string url = $"{_baseUrl}{version}/manifest.json";
        return await _linkFollower.FetchAsync<ReleaseManifest>(url, cancellationToken);
    }

    /// <summary>
    /// Gets the release history index (chronological view).
    /// URL: {baseUrl}/archives/index.json
    /// Cached after first fetch.
    /// </summary>
    public async Task<ReleaseHistoryIndex?> GetReleaseHistoryIndexAsync(CancellationToken cancellationToken = default)
    {
        string url = $"{_baseUrl}archives/index.json";
        return await _linkFollower.FetchAsync<ReleaseHistoryIndex>(url, cancellationToken);
    }

    /// <summary>
    /// Gets the history index for a specific year.
    /// URL: {baseUrl}/archives/{year}/index.json
    /// Cached after first fetch.
    /// </summary>
    /// <param name="year">Year (e.g., "2024", "2025")</param>
    public async Task<HistoryYearIndex?> GetYearIndexAsync(string year, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(year);
        string url = $"{_baseUrl}archives/{year}/index.json";
        return await _linkFollower.FetchAsync<HistoryYearIndex>(url, cancellationToken);
    }

    /// <summary>
    /// Gets the history index for a specific year and month.
    /// URL: {baseUrl}/archives/{year}/{month}/index.json
    /// Cached after first fetch.
    /// </summary>
    /// <param name="year">Year (e.g., "2024", "2025")</param>
    /// <param name="month">Month (e.g., "01", "12")</param>
    public async Task<HistoryMonthIndex?> GetMonthIndexAsync(string year, string month, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(year);
        ArgumentNullException.ThrowIfNullOrEmpty(month);
        string url = $"{_baseUrl}archives/{year}/{month}/index.json";
        return await _linkFollower.FetchAsync<HistoryMonthIndex>(url, cancellationToken);
    }

    // === Layer 3: High-Level Wrappers ===

    /// <summary>
    /// Gets a high-level summary of all .NET releases with support status.
    /// This replaces the functionality of the legacy releases-index.json file.
    /// The summary fetches data lazily on first use.
    /// </summary>
    public ReleasesSummary GetReleasesSummary()
    {
        return new ReleasesSummary(this);
    }

    /// <summary>
    /// Gets a navigator for deep exploration of a specific .NET version.
    /// The navigator fetches data lazily as needed.
    /// </summary>
    /// <param name="version">Major version (e.g., "8.0", "9.0")</param>
    public ReleaseNavigator GetReleaseNavigator(string version)
    {
        return new ReleaseNavigator(this, version);
    }

    /// <summary>
    /// Gets a high-level summary of release history archives with CVE information.
    /// The summary fetches data lazily on first use.
    /// </summary>
    public ArchivesSummary GetArchivesSummary()
    {
        return new ArchivesSummary(this);
    }

    /// <summary>
    /// Gets a navigator for deep exploration of a specific year's release history.
    /// The navigator fetches data lazily as needed.
    /// </summary>
    /// <param name="year">Year (e.g., "2024", "2025")</param>
    public ArchiveNavigator GetArchiveNavigator(string year)
    {
        return new ArchiveNavigator(this, year);
    }

    /// <summary>
    /// Follows a HAL link to fetch a document of the specified type.
    /// This is the core link-following pattern for navigating the graph.
    /// Cached after first fetch.
    /// </summary>
    /// <typeparam name="T">The type of document to deserialize</typeparam>
    /// <param name="link">The HAL link to follow</param>
    public Task<T?> FollowLinkAsync<T>(HalLink link, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(link);
        return _linkFollower.FetchAsync<T>(link.Href, cancellationToken);
    }
}
