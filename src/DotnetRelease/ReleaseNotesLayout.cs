using System.Text;
using DotnetRelease;

namespace DotnetRelease.Helper;

public class AdaptiveLayout(string root, HttpClient client) : IAdaptiveLayout
{
    private readonly string _location = root;
    private readonly IAdaptiveLayout _root = root.StartsWith("http") ?
        new WebLayout(root, client) :
        new FileLayout(root);

    public Task<Stream> GetStreamAsync(string uri) => _root.GetStreamAsync(uri);

    public string GetLocation(params Span<string> segments) => _root.GetLocation(segments);
}

public interface IAdaptiveLayout
{
    public string GetLocation(params Span<string> segments);

    public Task<Stream> GetStreamAsync(string uri);

    public static string MakeLocation(string root, char slash, Span<string> segments)
    {
        StringBuilder buffer = new();

        buffer.Append(root);

        foreach (string segment in segments)
        {
            buffer.Append(slash);
            buffer.Append(segment);
        }

        return buffer.ToString();
    }
}

public class FileLayout(string root) : IAdaptiveLayout
{
    private readonly string _root = root;

#pragma warning disable CS1998
    public async Task<Stream> GetStreamAsync(string uri) => File.OpenRead(uri);
#pragma warning restore CS1998

    public string GetLocation(params Span<string> segments) => IAdaptiveLayout.MakeLocation(_root, Path.DirectorySeparatorChar, segments);
}

public class WebLayout(string root, HttpClient client) : IAdaptiveLayout
{
    private readonly string _root = root;

    private readonly HttpClient _client = client;

    public Task<Stream> GetStreamAsync(string uri) => _client.GetStreamAsync(uri);

    public string GetLocation(params Span<string> segments) => IAdaptiveLayout.MakeLocation(_root, '/', segments);
}
