using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// Provides programmatic access to the .NET release notes graph via HAL+JSON navigation.
/// This is a minimal Layer 2 implementation focusing on document retrieval and link following.
/// </summary>
public class ReleaseNotesGraph
{
    private readonly string _baseUrl;
    private readonly HttpClient _client;

    public ReleaseNotesGraph(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _baseUrl = ReleaseNotes.GitHubBaseUri;
    }

    public ReleaseNotesGraph(HttpClient client, string baseUrl) : this(client)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(baseUrl);
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Gets the root major release version index containing all .NET versions.
    /// URL: {baseUrl}/index.json
    /// </summary>
    public async Task<MajorReleaseVersionIndex?> GetMajorReleaseIndexAsync(CancellationToken cancellationToken = default)
    {
        string url = $"{_baseUrl}index.json";
        return await FetchDocumentAsync<MajorReleaseVersionIndex>(url, ReleaseVersionIndexSerializerContext.Default.MajorReleaseVersionIndex, cancellationToken);
    }

    /// <summary>
    /// Gets the patch release index for a specific major version.
    /// URL: {baseUrl}/{version}/index.json
    /// </summary>
    /// <param name="version">Major version (e.g., "8.0", "9.0")</param>
    public async Task<PatchReleaseVersionIndex?> GetPatchReleaseIndexAsync(string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(version);
        string url = $"{_baseUrl}{version}/index.json";
        return await FetchDocumentAsync<PatchReleaseVersionIndex>(url, ReleaseVersionIndexSerializerContext.Default.PatchReleaseVersionIndex, cancellationToken);
    }

    /// <summary>
    /// Gets the manifest for a specific major version.
    /// URL: {baseUrl}/{version}/manifest.json
    /// </summary>
    /// <param name="version">Major version (e.g., "8.0", "9.0")</param>
    public async Task<ReleaseManifest?> GetManifestAsync(string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(version);
        string url = $"{_baseUrl}{version}/manifest.json";
        return await FetchDocumentAsync<ReleaseManifest>(url, ReleaseManifestSerializerContext.Default.ReleaseManifest, cancellationToken);
    }

    /// <summary>
    /// Gets the release history index (chronological view).
    /// URL: {baseUrl}/archives/index.json
    /// </summary>
    public async Task<ReleaseHistoryIndex?> GetReleaseHistoryIndexAsync(CancellationToken cancellationToken = default)
    {
        string url = $"{_baseUrl}archives/index.json";
        return await FetchDocumentAsync<ReleaseHistoryIndex>(url, ReleaseHistoryIndexSerializerContext.Default.ReleaseHistoryIndex, cancellationToken);
    }

    /// <summary>
    /// Gets the history index for a specific year.
    /// URL: {baseUrl}/archives/{year}/index.json
    /// </summary>
    /// <param name="year">Year (e.g., "2024", "2025")</param>
    public async Task<HistoryYearIndex?> GetYearIndexAsync(string year, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(year);
        string url = $"{_baseUrl}archives/{year}/index.json";
        return await FetchDocumentAsync<HistoryYearIndex>(url, HistoryYearIndexSerializerContext.Default.HistoryYearIndex, cancellationToken);
    }

    /// <summary>
    /// Gets the history index for a specific year and month.
    /// URL: {baseUrl}/archives/{year}/{month}/index.json
    /// </summary>
    /// <param name="year">Year (e.g., "2024", "2025")</param>
    /// <param name="month">Month (e.g., "01", "12")</param>
    public async Task<HistoryMonthIndex?> GetMonthIndexAsync(string year, string month, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(year);
        ArgumentNullException.ThrowIfNullOrEmpty(month);
        string url = $"{_baseUrl}archives/{year}/{month}/index.json";
        return await FetchDocumentAsync<HistoryMonthIndex>(url, HistoryYearIndexSerializerContext.Default.HistoryMonthIndex, cancellationToken);
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
    /// Follows a HAL link to fetch a document of the specified type.
    /// This is the core link-following pattern for navigating the graph.
    /// </summary>
    /// <typeparam name="T">The type of document to deserialize</typeparam>
    /// <param name="link">The HAL link to follow</param>
    public Task<T?> FollowLinkAsync<T>(HalLink link, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(link);

        // Route to the appropriate strongly-typed method based on type
        return typeof(T).Name switch
        {
            nameof(MajorReleaseVersionIndex) => FetchDocumentAsync(link.Href, ReleaseVersionIndexSerializerContext.Default.MajorReleaseVersionIndex, cancellationToken) as Task<T?>,
            nameof(PatchReleaseVersionIndex) => FetchDocumentAsync(link.Href, ReleaseVersionIndexSerializerContext.Default.PatchReleaseVersionIndex, cancellationToken) as Task<T?>,
            nameof(ReleaseManifest) => FetchDocumentAsync(link.Href, ReleaseManifestSerializerContext.Default.ReleaseManifest, cancellationToken) as Task<T?>,
            nameof(ReleaseHistoryIndex) => FetchDocumentAsync(link.Href, ReleaseHistoryIndexSerializerContext.Default.ReleaseHistoryIndex, cancellationToken) as Task<T?>,
            nameof(HistoryYearIndex) => FetchDocumentAsync(link.Href, HistoryYearIndexSerializerContext.Default.HistoryYearIndex, cancellationToken) as Task<T?>,
            nameof(HistoryMonthIndex) => FetchDocumentAsync(link.Href, HistoryYearIndexSerializerContext.Default.HistoryMonthIndex, cancellationToken) as Task<T?>,
            nameof(SdkVersionIndex) => FetchDocumentAsync(link.Href, SdkVersionIndexSerializerContext.Default.SdkVersionIndex, cancellationToken) as Task<T?>,
            _ => throw new NotSupportedException($"Type {typeof(T).Name} is not supported for link following")
        } ?? throw new InvalidOperationException($"Failed to cast result to {typeof(T).Name}");
    }

    private async Task<T?> FetchDocumentAsync<T>(string url, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken) where T : class
    {
        try
        {
            using var stream = await _client.GetStreamAsync(url, cancellationToken);
            return await JsonSerializer.DeserializeAsync(stream, typeInfo, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
