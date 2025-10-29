using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using DotnetRelease;
using CveInfo;

List<ModelInfo> models = [
    new (typeof(MajorReleasesIndex), "dotnet-releases-index.json"),
    new (typeof(MajorReleaseOverview), "dotnet-releases.json"),
    new (typeof(PatchReleasesIndex), "dotnet-patch-releases-index.json"),
    new (typeof(PatchReleaseOverview), "dotnet-patch-release.json"),
    new (typeof(OSPackagesOverview), "dotnet-os-packages.json"),
    new (typeof(SupportedOSMatrix), "dotnet-supported-os-matrix.json"),
    new (typeof(CveSet), "dotnet-cves.json", JsonKnownNamingPolicy.SnakeCaseLower),
    // new (typeof(ReportOverview), "dotnet-support-report.json"),
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

void WriteSchema(ModelInfo modelInfo)
{
    var (type, targetFile, namingPolicy) = modelInfo;
    var serializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = namingPolicy switch
        {
            JsonKnownNamingPolicy.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
            _ => JsonNamingPolicy.KebabCaseLower
        },
        TypeInfoResolver = type == typeof(CveSet) 
            ? CveSchemaGenerationContext.Default 
            : SchemaGenerationContext.Default
    };
    var schema = JsonSchemaExporter.GetJsonSchemaAsNode(serializerOptions, type, exporterOptions);
    File.WriteAllText(targetFile, schema.ToString());
}

static TAttribute? GetCustomAttribute<TAttribute>(ICustomAttributeProvider? provider, bool inherit = false) where TAttribute : Attribute
    => provider?.GetCustomAttributes(typeof(TAttribute), inherit).FirstOrDefault() as TAttribute;

record ModelInfo(Type Type, string TargetFile, JsonKnownNamingPolicy NamingPolicy = JsonKnownNamingPolicy.KebabCaseLower);

[JsonSerializable(typeof(MajorReleasesIndex))]
[JsonSerializable(typeof(MajorReleaseOverview))]
[JsonSerializable(typeof(PatchReleasesIndex))]
[JsonSerializable(typeof(PatchReleaseOverview))]
[JsonSerializable(typeof(OSPackagesOverview))]
[JsonSerializable(typeof(SupportedOSMatrix))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
partial class SchemaGenerationContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(CveSet))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
partial class CveSchemaGenerationContext : JsonSerializerContext
{
}
