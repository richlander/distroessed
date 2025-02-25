namespace FileHelpers;

public class WebPath(string basePath, HttpClient client) : IAdaptivePath
{
    private readonly string _base = basePath;

    private readonly HttpClient _client = client;

    public Task<Stream> GetStreamAsync(string uri) => _client.GetStreamAsync(uri);

    public string Combine(params Span<string> segments) => AdaptivePath.Combine(_base, '/', segments);

    public bool CanHandlePath(string path) => path.StartsWith("http://") || path.StartsWith("https://");

    public bool SupportsLocalPaths => false;
}
