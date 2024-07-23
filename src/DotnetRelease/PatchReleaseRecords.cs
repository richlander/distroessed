using System.ComponentModel;

namespace DotnetRelease;

public record PatchReleaseOverview(
    [property: Description("Major (or major.minor) version of product.")]
    string ChannelVersion,

    [property: Description("A patch release with detailed release information.")]
    PatchRelease Release);
