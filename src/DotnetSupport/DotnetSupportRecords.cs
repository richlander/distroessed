namespace DotnetSupport;

public record SupportMatrix(string ChannelVersion, DateOnly LastUpdated, IList<SupportFamily> Families, IList<SupportLibc> Libc, IList<string> Notes);

public record SupportFamily(string Name, IList<SupportDistribution> Distributions); 

public record SupportDistribution(string Id, string Name, string Link, string Lifecycle, IList<string> Architectures, IList<string> SupportedCycles)
{
    public IList<string>? Notes { get; set; }
}

public record SupportLibc(string Name, IList<string> Architectures, string Version, string Source);
