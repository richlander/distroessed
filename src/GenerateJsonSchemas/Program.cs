using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using DotnetRelease;

List<ModelInfo> models = [
    new (typeof(MajorReleasesIndex), JsonSchemaContext.Default.MajorReleasesIndex, "dotnet-releases-index.json"),
    new (typeof(MajorReleaseOverview), JsonSchemaContext.Default.MajorReleaseOverview, "dotnet-releases.json"),
    new (typeof(PatchReleasesIndex), JsonSchemaContext.Default.PatchReleasesIndex, "dotnet-patch-releases-index.json"),
    new (typeof(PatchReleaseOverview), JsonSchemaContext.Default.PatchReleaseOverview, "dotnet-patch-release.json"),
    new (typeof(OSPackagesOverview), JsonSchemaContext.Default.OSPackagesOverview, "dotnet-os-packages.json"),
    new (typeof(SupportedOSMatrix), JsonSchemaContext.Default.SupportedOSMatrix, "dotnet-supported-os-matrix.json"),
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