using System.Text.Json.Serialization;

namespace DotnetRelease;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(MajorReleasesIndex))]
public partial class MajorReleasesIndexSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(MajorReleaseOverview))]
public partial class MajorReleaseOverviewSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(PatchReleasesIndex))]
public partial class PatchReleasesIndexSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(PatchReleaseOverview))]
public partial class PatchReleaseOverviewSerializerContext : JsonSerializerContext
{
}


[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(OSPackagesOverview))]
public partial class OSPackagesSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(SupportedOSMatrix))]
public partial class SupportedOSMatrixSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(ReleaseIndex))]
public partial class ReleaseIndexSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(ReleaseManifest))]
public partial class ReleaseManifestSerializerContext : JsonSerializerContext
{
}
