using System.Text;

namespace FileHelpers;

public class AdaptivePath
{
    public static IAdaptivePath GetFromDefaultAdaptors(string basePath, HttpClient client) =>
        GetAdaptor(basePath, new WebPath(basePath, client), new FilePath(basePath));

    public static IAdaptivePath GetAdaptor(string basePath, params Span<IAdaptivePath> adaptors)
    {
        foreach (IAdaptivePath adaptor in adaptors)
        {
            if (adaptor.CanHandlePath(basePath))
            {
                return adaptor;
            }
        }

        throw new NotSupportedException($"No adaptor found for path: {basePath}");
    }

    public static string Combine(string root, char slash, Span<string> segments)
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
