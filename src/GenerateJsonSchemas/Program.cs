using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using DotnetRelease;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: GenerateJsonSchemas <output-directory>");
    return 1;
}

var outputDir = args[0];
if (!Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
}

List<ModelInfo> models = [
    new (typeof(MajorReleasesIndex), JsonSchemaContext.Default.MajorReleasesIndex, Path.Combine(outputDir, "dotnet-releases-index.json")),
    new (typeof(MajorReleaseOverview), JsonSchemaContext.Default.MajorReleaseOverview, Path.Combine(outputDir, "dotnet-releases.json")),
    new (typeof(PatchReleasesIndex), JsonSchemaContext.Default.PatchReleasesIndex, Path.Combine(outputDir, "dotnet-patch-releases-index.json")),
    new (typeof(PatchReleaseOverview), JsonSchemaContext.Default.PatchReleaseOverview, Path.Combine(outputDir, "dotnet-patch-release.json")),
    new (typeof(OSPackagesOverview), JsonSchemaContext.Default.OSPackagesOverview, Path.Combine(outputDir, "dotnet-os-packages.json")),
    new (typeof(SupportedOSMatrix), JsonSchemaContext.Default.SupportedOSMatrix, Path.Combine(outputDir, "dotnet-supported-os-matrix.json")),
    // HAL+JSON schemas
    new (typeof(ReleaseVersionIndex), HalJsonSchemaContext.Default.ReleaseVersionIndex, Path.Combine(outputDir, "release-version-index.json")),
    new (typeof(ReleaseHistoryIndex), HalJsonSchemaContext.Default.ReleaseHistoryIndex, Path.Combine(outputDir, "release-history-index.json")),
    new (typeof(HistoryYearIndex), HalJsonSchemaContext.Default.HistoryYearIndex, Path.Combine(outputDir, "history-year-index.json")),
    new (typeof(HistoryMonthIndex), HalJsonSchemaContext.Default.HistoryMonthIndex, Path.Combine(outputDir, "history-month-index.json")),
    new (typeof(ReleaseManifest), HalJsonSchemaContext.Default.ReleaseManifest, Path.Combine(outputDir, "release-manifest.json")),
];

var exporterOptions = new JsonSchemaExporterOptions()
    {
        
        TransformSchemaNode = (ctx, schema) =>
        {
            if (schema is not JsonObject schemaObj || schemaObj.ContainsKey("$ref"))
            {
                return schema;
            }

            DescriptionAttribute? descriptionAttribute =
                GetCustomAttribute<DescriptionAttribute>(ctx.PropertyInfo?.AttributeProvider) ??
                GetCustomAttribute<DescriptionAttribute>(ctx.PropertyInfo?.AssociatedParameter?.AttributeProvider) ??
                GetCustomAttribute<DescriptionAttribute>(ctx.TypeInfo.Type);

            if (descriptionAttribute != null)
            {
                schemaObj.Insert(0, "description", (JsonNode)descriptionAttribute.Description);
            }

            return schemaObj;
        }

    };

foreach (var model in models)
{
    WriteSchema(model);
}

Console.WriteLine($"Generated {models.Count} JSON schemas in {outputDir}");
return 0;

void WriteSchema(ModelInfo modelInfo)
{
    var (type, typeInfo, targetFile) = modelInfo;
    var schema = JsonSchemaExporter.GetJsonSchemaAsNode(typeInfo, exporterOptions);
    File.WriteAllText(targetFile, schema.ToString());
}

static TAttribute? GetCustomAttribute<TAttribute>(ICustomAttributeProvider? provider, bool inherit = false) where TAttribute : Attribute
    => provider?.GetCustomAttributes(typeof(TAttribute), inherit).FirstOrDefault() as TAttribute;

record ModelInfo(Type Type, JsonTypeInfo TypeInfo, string TargetFile);


[JsonSerializable(typeof(MajorReleasesIndex))]
[JsonSerializable(typeof(MajorReleaseOverview))]
[JsonSerializable(typeof(PatchReleasesIndex))]
[JsonSerializable(typeof(PatchReleaseOverview))]
[JsonSerializable(typeof(OSPackagesOverview))]
[JsonSerializable(typeof(SupportedOSMatrix))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower, WriteIndented = true)]
public partial class JsonSchemaContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(ReleaseVersionIndex))]
[JsonSerializable(typeof(ReleaseHistoryIndex))]
[JsonSerializable(typeof(HistoryYearIndex))]
[JsonSerializable(typeof(HistoryMonthIndex))]
[JsonSerializable(typeof(ReleaseManifest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower, WriteIndented = true)]
public partial class HalJsonSchemaContext : JsonSerializerContext
{
}