using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using DotnetRelease;

var serializerOptions = new JsonSerializerOptions(JsonSerializerOptions.Default)
{
    PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower
};

var exporterOptions = new JsonSchemaExporterOptions()
    {
        
        TransformSchemaNode = static (ctx, schema) =>
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

List<Tuple<Type, string>> models = [
    new (typeof(PackageOverview), "dotnet-os-packages.json"),
    new (typeof(SupportMatrix), "dotnet-support-matrix.json"),
    new (typeof(ReportOverview), "dotnet-support-report.json"),
    new (typeof(ReleaseIndexOverview), "dotnet-release-index.json")
];

foreach (var model in models)
{
    var (type, file ) = model;

    WriteSchema(type, file);
}

void WriteSchema(Type type, string targetFile)
{
    var schema = JsonSchemaExporter.GetJsonSchemaAsNode(serializerOptions, type, exporterOptions);
    File.WriteAllText(targetFile, schema.ToString());
}

static TAttribute? GetCustomAttribute<TAttribute>(ICustomAttributeProvider? provider, bool inherit = false) where TAttribute : Attribute
    => provider?.GetCustomAttributes(typeof(TAttribute), inherit).FirstOrDefault() as TAttribute;

