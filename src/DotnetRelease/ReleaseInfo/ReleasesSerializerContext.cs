using System.Text.Json.Serialization;
using DotnetRelease.Support;

namespace DotnetRelease.ReleaseInfo;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(MajorReleaseOverview))]
public partial class MajorReleaseOverviewSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(PatchReleaseOverview))]
public partial class PatchReleaseOverviewSerializerContext : JsonSerializerContext
{
}


[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(OSPackagesOverview))]
public partial class OSPackagesSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(SupportedOSMatrix))]
public partial class SupportedOSMatrixSerializerContext : JsonSerializerContext
{
}
