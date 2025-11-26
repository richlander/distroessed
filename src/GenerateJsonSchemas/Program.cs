using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using DotnetRelease.Graph;
using DotnetRelease.Index;
using DotnetRelease.ReleaseInfo;
using DotnetRelease.Support;
using DotnetRelease.Security;

// GenerateJsonSchemas - Generate JSON Schema files for data models

Console.WriteLine("GenerateJsonSchemas");

if (args.Length < 2 || args[0] != "generate")
{
    ReportInvalidArgs();
    return 1;
}

string targetDirectory = args[1];

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
    // Legacy schemas (kebab-case)
    new (typeof(MajorReleasesIndex), DotnetRelease.FileNames.Schemas.ReleasesIndex),
    new (typeof(MajorReleaseOverview), DotnetRelease.FileNames.Schemas.Releases),
    new (typeof(PatchReleaseOverview), DotnetRelease.FileNames.Schemas.PatchRelease),
    new (typeof(OSPackagesOverview), DotnetRelease.FileNames.Schemas.OsPackages),
    new (typeof(SupportedOSMatrix), DotnetRelease.FileNames.Schemas.SupportedOs),
    new (typeof(CveRecords), DotnetRelease.FileNames.Schemas.Cves, JsonKnownNamingPolicy.SnakeCaseLower),

    // New index schemas (snake_case)
    new (typeof(MajorReleaseVersionIndex), DotnetRelease.FileNames.Schemas.ReleaseVersionIndex, JsonKnownNamingPolicy.SnakeCaseLower),
    new (typeof(ReleaseHistoryIndex), DotnetRelease.FileNames.Schemas.TimelineIndex, JsonKnownNamingPolicy.SnakeCaseLower),
    new (typeof(PatchDetailIndex), DotnetRelease.FileNames.Schemas.PatchDetailIndex, JsonKnownNamingPolicy.SnakeCaseLower),
    new (typeof(SdkVersionIndex), DotnetRelease.FileNames.Schemas.SdkVersionIndex, JsonKnownNamingPolicy.SnakeCaseLower),
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
        TypeInfoResolver = GetTypeInfoResolver(type, namingPolicy)
    };
    var schema = JsonSchemaExporter.GetJsonSchemaAsNode(serializerOptions, type, exporterOptions);
    File.WriteAllText(outputPath, schema.ToString());
    Console.WriteLine($"  ✓ {targetFile}");
}

System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver GetTypeInfoResolver(Type type, JsonKnownNamingPolicy namingPolicy)
{
    // CVE records use snake_case with dedicated context
    if (type == typeof(CveRecords))
        return CveSchemaGenerationContext.Default;

    // New index types use snake_case with dedicated context
    if (type == typeof(MajorReleaseVersionIndex) ||
        type == typeof(ReleaseHistoryIndex) ||
        type == typeof(PatchDetailIndex) ||
        type == typeof(SdkVersionIndex))
        return IndexSchemaGenerationContext.Default;

    // Legacy types use kebab-case
    return SchemaGenerationContext.Default;
}

static TAttribute? GetCustomAttribute<TAttribute>(ICustomAttributeProvider? provider, bool inherit = false) where TAttribute : Attribute
    => provider?.GetCustomAttributes(typeof(TAttribute), inherit).FirstOrDefault() as TAttribute;

static void ReportInvalidArgs()
{
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  GenerateJsonSchemas generate <target-directory>");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  generate            Generate JSON schema files");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  <target-directory>  Directory where schema files will be written");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  GenerateJsonSchemas generate ./schemas");
    Console.WriteLine("  GenerateJsonSchemas generate ~/git/core/release-notes/schemas");
}

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

[JsonSerializable(typeof(MajorReleaseVersionIndex))]
[JsonSerializable(typeof(ReleaseHistoryIndex))]
[JsonSerializable(typeof(PatchDetailIndex))]
[JsonSerializable(typeof(SdkVersionIndex))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
partial class IndexSchemaGenerationContext : JsonSerializerContext
{
}
