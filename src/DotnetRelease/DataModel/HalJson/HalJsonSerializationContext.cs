using System.Text.Json.Serialization;

namespace DotnetRelease;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(ReleaseVersionIndex))]
[JsonSerializable(typeof(Lifecycle))]
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
