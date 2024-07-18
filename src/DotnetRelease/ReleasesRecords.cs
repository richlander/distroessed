namespace DotnetRelease;

public record ReleasesOverview(string ChannelVersion, DateOnly EolDate, IList<Release> Releases);

public record Release(DateOnly ReleaseDate, string ReleaseVersion);