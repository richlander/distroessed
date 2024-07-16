namespace DotnetRelease;

public record ReleaseOverview(string ChannelVersion, DateOnly EolDate, IList<Release> Releases);

public record Release(DateOnly ReleaseDate, string ReleaseVersion);