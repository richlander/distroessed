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
    Console.Error.WriteLine("Usage: GenerateJsonSchemas <target_directory>");
    return 1;
}

string targetDirectory = args[0];
if (!Directory.Exists(targetDirectory))
{
    Directory.CreateDirectory(targetDirectory);
}

List<ModelInfo> models = [
    new (typeof(MajorReleasesIndex), MajorReleasesIndexSerializerContext.Default.MajorReleasesIndex, "dotnet-releases-index.json"),
    new (typeof(MajorReleaseOverview), MajorReleaseOverviewSerializerContext.Default.MajorReleaseOverview, "dotnet-releases.json"),
    new (typeof(PatchReleaseOverview), PatchReleaseOverviewSerializerContext.Default.PatchReleaseOverview, "dotnet-patch-release.json"),
    new (typeof(OSPackagesOverview), OSPackagesSerializerContext.Default.OSPackagesOverview, "dotnet-os-packages.json"),
    new (typeof(SupportedOSMatrix), SupportedOSMatrixSerializerContext.Default.SupportedOSMatrix, "dotnet-supported-os-matrix.json"),
    new (typeof(ReleaseVersionIndex), ReleaseVersionIndexSerializerContext.Default.ReleaseVersionIndex, "dotnet-release-version-index.json"),
    new (typeof(ReleaseHistoryIndex), ReleaseHistoryIndexSerializerContext.Default.ReleaseHistoryIndex, "dotnet-release-history-index.json"),
    new (typeof(ReleaseManifest), ReleaseManifestSerializerContext.Default.ReleaseManifest, "dotnet-release-manifest.json"),
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

return 0;

void WriteSchema(ModelInfo modelInfo)
{
    var (type, typeInfo, targetFile) = modelInfo;
    var schema = JsonSchemaExporter.GetJsonSchemaAsNode(typeInfo, exporterOptions);
    var outputPath = Path.Combine(targetDirectory, targetFile);
    File.WriteAllText(outputPath, schema.ToString());
    Console.WriteLine($"Generated schema: {outputPath}");
}

static TAttribute? GetCustomAttribute<TAttribute>(ICustomAttributeProvider? provider, bool inherit = false) where TAttribute : Attribute
    => provider?.GetCustomAttributes(typeof(TAttribute), inherit).FirstOrDefault() as TAttribute;

record ModelInfo(Type Type, JsonTypeInfo TypeInfo, string TargetFile);


