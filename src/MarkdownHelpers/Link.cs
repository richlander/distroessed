namespace MarkdownHelpers;

public class Link(int startingIndex = 0)
{
    private readonly Dictionary<string, string> _links = [];

    public int Index { get; private set; } = startingIndex;

    public int Count => _links.Count;

    public string AddReferenceLink(string display, string url, string reference)
    {
        if (!_links.TryGetValue(url, out var value))
        {
            _links.Add(url, reference);
        }

        return MakeReferenceStyle(display, reference);
    }

    public string AddReferenceLink(string display, string url) => AddReferenceLink(display, url, display);

    public string AddIndexReferenceLink(string display, string url)
    {
        if (!_links.TryGetValue(url, out var value))
        {
            value = Index.ToString();
            _links.Add(url, value);
            Index++;
        }

        return MakeReferenceStyle(display, value.ToString());
    }

    public IEnumerable<string> GetReferenceLinkAnchors()
    {
        foreach (var link in _links.Keys)
        {
            yield return MakeReferenceAnchor(_links[link], link);
        }
    }
    
    public static string Make(string display, string url) => $"[{display}]({url})";

    public static string MakeReferenceStyle(string display, string reference) => $"[{display}][{reference}]";

    public static string MakeReferenceAnchor(string reference, string url) => $"[{reference}]: {url}";
    
}