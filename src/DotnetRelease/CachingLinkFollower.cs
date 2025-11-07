using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using DotnetRelease.Cves;
using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// ILinkFollower implementation that caches fetched documents by URL.
/// Thread-safe for concurrent access.
/// </summary>
public class CachingLinkFollower : ILinkFollower
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly ConcurrentDictionary<string, object> _cache;

    public CachingLinkFollower(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _baseUrl = ReleaseNotes.GitHubBaseUri;
        _cache = new ConcurrentDictionary<string, object>();
    }

    public CachingLinkFollower(HttpClient client, string baseUrl) : this(client)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(baseUrl);
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Fetches a document from the specified URL with caching.
    /// First call fetches from network, subsequent calls return cached instance.
    /// </summary>
    public async Task<T?> FetchAsync<T>(string href, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNullOrEmpty(href);

        // Check cache first
        if (_cache.TryGetValue(href, out var cached))
        {
            return (T)cached;
        }

        // Determine which serializer context to use based on type
        var typeInfo = GetTypeInfo<T>();

        // Fetch from network
        var document = await FetchDocumentAsync(href, typeInfo, cancellationToken);

        // Store in cache (even if null)
        if (document is not null)
        {
            _cache[href] = document;
        }

        return document;
    }

    /// <summary>
    /// Clears all cached documents.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Attempts to remove a specific URL from the cache.
    /// </summary>
    /// <param name="href">The URL to evict</param>
    /// <returns>True if the entry was found and removed</returns>
    public bool TryEvict(string href)
    {
        return _cache.TryRemove(href, out _);
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

    private static JsonTypeInfo<T> GetTypeInfo<T>() where T : class
    {
        // Map types to their appropriate serializer contexts
        return typeof(T).Name switch
        {
            nameof(MajorReleaseVersionIndex) => (JsonTypeInfo<T>)(object)ReleaseVersionIndexSerializerContext.Default.MajorReleaseVersionIndex,
            nameof(PatchReleaseVersionIndex) => (JsonTypeInfo<T>)(object)ReleaseVersionIndexSerializerContext.Default.PatchReleaseVersionIndex,
            nameof(ReleaseManifest) => (JsonTypeInfo<T>)(object)ReleaseManifestSerializerContext.Default.ReleaseManifest,
            nameof(ReleaseHistoryIndex) => (JsonTypeInfo<T>)(object)ReleaseHistoryIndexSerializerContext.Default.ReleaseHistoryIndex,
            nameof(HistoryYearIndex) => (JsonTypeInfo<T>)(object)HistoryYearIndexSerializerContext.Default.HistoryYearIndex,
            nameof(HistoryMonthIndex) => (JsonTypeInfo<T>)(object)HistoryYearIndexSerializerContext.Default.HistoryMonthIndex,
            nameof(SdkVersionIndex) => (JsonTypeInfo<T>)(object)SdkVersionIndexSerializerContext.Default.SdkVersionIndex,
            nameof(CveRecords) => (JsonTypeInfo<T>)(object)CveSerializerContext.Default.CveRecords,
            _ => throw new NotSupportedException($"Type {typeof(T).Name} is not supported for deserialization")
        };
    }
}
