namespace DotnetRelease;

public record HalLink
{
    public string Href { get; init; } = default!;

    public string? Relative { get; init; }

    public string? Title { get; init; }

    public string? Type { get; init; }
}
