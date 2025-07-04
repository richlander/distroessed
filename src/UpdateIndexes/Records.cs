using DotnetRelease;

public record ReleaseKindMapping(string Name, string Filename, ReleaseKind Kind, string FileType);

public record FileLink(string File, string Title, LinkStyle Style);

[Flags]
public enum LinkStyle
{
    Prod = 1 << 0,
    GitHub = 1 << 1,
}
