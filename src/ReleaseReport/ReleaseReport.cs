using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReleaseReport;

public class Release
{
    public static string WriteReport(Report report) => JsonSerializer.Serialize(report, ReportJsonSerializerContext.Default.Report);
}


[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Serialization,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true
    )]
[JsonSerializable(typeof(Report))]
internal partial class ReportJsonSerializerContext : JsonSerializerContext
{
}