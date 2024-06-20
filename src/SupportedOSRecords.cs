using System.Text.Json.Serialization;

namespace DotnetSupport;

public record SupportMatrix(string ChannelVersion, DateOnly LastUpdated, IList<SupportFamily> Families);

public record SupportFamily(string Name, IList<SupportDistribution> Distributions); 

public record SupportDistribution(string Id, string Name, string LifecyclePolicy, IList<string> Architectures, IList<string> SupportedCycles);
