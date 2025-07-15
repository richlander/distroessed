using System.Text.Json.Serialization;

namespace DotnetRelease;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(ReleaseVersionIndex))]
[JsonSerializable(typeof(Support))]
[JsonSerializable(typeof(CveRecordSummary))]
public partial class ReleaseVersionIndexSerializerContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]

[JsonSerializable(typeof(ReleaseManifest))]
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
[JsonSerializable(typeof(ReleaseHistoryYearIndex))]
[JsonSerializable(typeof(ReleaseHistoryMonthIndex))]
[JsonSerializable(typeof(ReleaseHistoryMonthSummary))]
[JsonSerializable(typeof(CveRecords))]
[JsonSerializable(typeof(CveRecordsSummary))]
[JsonSerializable(typeof(CveRecordSummary))]
public partial class ReleaseHistoryYearIndexSerializerContext : JsonSerializerContext
{
}
