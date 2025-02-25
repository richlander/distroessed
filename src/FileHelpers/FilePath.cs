namespace FileHelpers;

public class FilePath(string basePath) : IAdaptivePath
{
    private readonly string _base = basePath;

#pragma warning disable CS1998
    public async Task<Stream> GetStreamAsync(string uri) => File.OpenRead(uri);
#pragma warning restore CS1998

    public string Combine(params Span<string> segments) => AdaptivePath.Combine(_base, Path.DirectorySeparatorChar, segments);

    public bool CanHandlePath(string path) => true;

    public bool SupportsLocalPaths => true;

}