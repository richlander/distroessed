using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using DotnetRelease.Index;
using DotnetRelease.ReleaseInfo;
using DotnetRelease.Support;
using DotnetRelease.Security;

// GenerateJsonSchemas - Generate JSON Schema files for data models
// Usage:
//   GenerateJsonSchemas [target-directory]
//   target-directory: Directory where schema files will be written (optional, defaults to current directory)

string targetDirectory = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

if (!Directory.Exists(targetDirectory))
{
    try
    {
        Directory.CreateDirectory(targetDirectory);
        Console.WriteLine($"Created target directory: {targetDirectory}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: Could not create target directory '{targetDirectory}': {ex.Message}");
        return 1;
    }
}

Console.WriteLine($"Generating JSON schemas in: {targetDirectory}");
Console.WriteLine();

List<ModelInfo> models = [
    new (typeof(MajorReleasesIndex), "dotnet-releases-index.json"),
    new (typeof(MajorReleaseOverview), "dotnet-releases.json"),
    new (typeof(PatchReleaseOverview), "dotnet-patch-release.json"),
    new (typeof(OSPackagesOverview), "dotnet-os-packages.json"),
    new (typeof(SupportedOSMatrix), "dotnet-supported-os-matrix.json"),
    new (typeof(CveRecords), "dotnet-cves.json", JsonKnownNamingPolicy.SnakeCaseLower),
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

Console.WriteLine($"Generated {models.Count} schema file(s)");
return 0;

void WriteSchema(ModelInfo modelInfo)
{
    var (type, targetFile, namingPolicy) = modelInfo;
    var outputPath = Path.Combine(targetDirectory, targetFile);
    var serializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = namingPolicy switch
        {
            JsonKnownNamingPolicy.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
            _ => JsonNamingPolicy.KebabCaseLower
        },
        TypeInfoResolver = type == typeof(CveRecords) 
            ? CveSchemaGenerationContext.Default 
            : SchemaGenerationContext.Default
    };
    var schema = JsonSchemaExporter.GetJsonSchemaAsNode(serializerOptions, type, exporterOptions);
    File.WriteAllText(outputPath, schema.ToString());
    Console.WriteLine($"  ✓ {targetFile}");
}

static TAttribute? GetCustomAttribute<TAttribute>(ICustomAttributeProvider? provider, bool inherit = false) where TAttribute : Attribute
    => provider?.GetCustomAttributes(typeof(TAttribute), inherit).FirstOrDefault() as TAttribute;

record ModelInfo(Type Type, string TargetFile, JsonKnownNamingPolicy NamingPolicy = JsonKnownNamingPolicy.KebabCaseLower);

[JsonSerializable(typeof(MajorReleasesIndex))]
[JsonSerializable(typeof(MajorReleaseOverview))]
[JsonSerializable(typeof(PatchReleaseOverview))]
[JsonSerializable(typeof(OSPackagesOverview))]
[JsonSerializable(typeof(SupportedOSMatrix))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
partial class SchemaGenerationContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(CveRecords))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
partial class CveSchemaGenerationContext : JsonSerializerContext
{
}
