using System.Text.Json.Serialization;

namespace DotnetRelease;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(MajorReleaseVersionIndex))]
[JsonSerializable(typeof(PatchReleaseVersionIndex))]
[JsonSerializable(typeof(ReleaseVersionIndex))]
[JsonSerializable(typeof(MajorReleaseVersionIndexEntry))]
[JsonSerializable(typeof(PatchReleaseVersionIndexEntry))]
[JsonSerializable(typeof(ReleaseVersionIndexEntry))]
[JsonSerializable(typeof(Lifecycle))]
[JsonSerializable(typeof(PatchLifecycle))]
[JsonSerializable(typeof(CveRecordSummary))]
public partial class ReleaseVersionIndexSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]

[JsonSerializable(typeof(ReleaseManifest))]
[JsonSerializable(typeof(PartialManifest))]
public partial class ReleaseManifestSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(ReleaseHistoryIndex))]
[JsonSerializable(typeof(ReleaseMetadata))]
public partial class ReleaseHistoryIndexSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(HistoryYearIndex))]
[JsonSerializable(typeof(HistoryMonthIndex))]
[JsonSerializable(typeof(HistoryMonthSummary))]
[JsonSerializable(typeof(CveRecords))]
[JsonSerializable(typeof(CveRecordsSummary))]
[JsonSerializable(typeof(CveRecordSummary))]
public partial class HistoryYearIndexSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(SdkVersionIndex))]
[JsonSerializable(typeof(SdkDownloadInfo))]
public partial class SdkVersionIndexSerializerContext : JsonSerializerContext
{
}
