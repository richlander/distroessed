using DotnetRelease;

public class ReleaseNotesGraph
{
    private string _baseUrl = ReleaseNotes.GitHubBaseUri;
    private HttpClient _client;

    public ReleaseNotesGraph(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    public ReleaseNotesGraph(HttpClient client, string baseUrl) : this(client)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(baseUrl);
        _baseUrl = baseUrl;
    }

}
