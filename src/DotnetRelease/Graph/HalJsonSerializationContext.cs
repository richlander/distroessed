using System.Text.Json.Serialization;
using DotnetRelease.Security;

namespace DotnetRelease.Graph;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(MajorReleaseVersionIndex))]
[JsonSerializable(typeof(PatchReleaseVersionIndex))]
[JsonSerializable(typeof(PatchDetailIndex))]
[JsonSerializable(typeof(PatchDetailIndexEmbedded))]
[JsonSerializable(typeof(SdkFeatureBandEntry))]
[JsonSerializable(typeof(ReleaseVersionIndex))]
[JsonSerializable(typeof(MajorReleaseVersionIndexEntry))]
[JsonSerializable(typeof(PatchReleaseVersionIndexEntry))]
[JsonSerializable(typeof(ReleaseVersionIndexEntry))]
[JsonSerializable(typeof(TimelineYear))]
[JsonSerializable(typeof(Lifecycle))]
[JsonSerializable(typeof(PatchLifecycle))]
[JsonSerializable(typeof(CveRecordSummary))]
public partial class ReleaseVersionIndexSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]

[JsonSerializable(typeof(ReleaseManifest))]
[JsonSerializable(typeof(PartialManifest))]
[JsonSerializable(typeof(ContentManifest))]
[JsonSerializable(typeof(PartialContentManifest))]
public partial class ReleaseManifestSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(ReleaseHistoryIndex))]
[JsonSerializable(typeof(ReleaseMetadata))]
public partial class ReleaseHistoryIndexSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(HistoryYearIndex))]
[JsonSerializable(typeof(HistoryMonthIndex))]
[JsonSerializable(typeof(HistoryMonthSummary))]
[JsonSerializable(typeof(MajorReleaseVersionIndexEntry))]
[JsonSerializable(typeof(Lifecycle))]
[JsonSerializable(typeof(CveRecords))]
[JsonSerializable(typeof(CveRecordsSummary))]
[JsonSerializable(typeof(CveRecordSummary))]
[JsonSerializable(typeof(CommitLink))]
[JsonSerializable(typeof(HalLink))]
public partial class HistoryYearIndexSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(SdkVersionIndex))]
[JsonSerializable(typeof(SdkDownloadInfo))]
[JsonSerializable(typeof(SdkDownloadEmbedded))]
[JsonSerializable(typeof(SdkDownloadFile))]
public partial class SdkVersionIndexSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(DownloadsIndex))]
[JsonSerializable(typeof(DownloadsIndexEmbedded))]
[JsonSerializable(typeof(ComponentEntry))]
[JsonSerializable(typeof(FeatureBandEntry))]
[JsonSerializable(typeof(ComponentDownload))]
[JsonSerializable(typeof(ComponentDownloadEmbedded))]
[JsonSerializable(typeof(DownloadFile))]
public partial class DownloadsIndexSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(TargetFrameworksIndex))]
[JsonSerializable(typeof(TargetFrameworkEntry))]
public partial class TargetFrameworksSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(LlmsIndex))]
[JsonSerializable(typeof(LlmsIndexEmbedded))]
[JsonSerializable(typeof(LlmsPatchEntry))]
[JsonSerializable(typeof(HistoryMonthSummary))]
[JsonSerializable(typeof(PartialLlmsIndex))]
public partial class LlmsIndexSerializerContext : JsonSerializerContext
{
}
