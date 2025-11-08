using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease.Summary;

public class ReleaseReport
{
    public static string WriteReport(ReportOverview report) => JsonSerializer.Serialize(report, ReportJsonSerializerContext.Default.ReportOverview);
}


[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Serialization,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true
    )]
[JsonSerializable(typeof(ReportOverview))]
internal partial class ReportJsonSerializerContext : JsonSerializerContext
{
}