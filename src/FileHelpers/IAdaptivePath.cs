using System.Text;

namespace FileHelpers;

public interface IAdaptivePath
{
    public bool CanHandlePath(string path);

    public bool SupportsLocalPaths { get; }

    public string Combine(params Span<string> segments);

    public Task<Stream> GetStreamAsync(string uri);
}