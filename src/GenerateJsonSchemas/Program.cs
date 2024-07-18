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

var packageSchema = JsonSchemaExporter.GetJsonSchemaAsNode(JsonSerializerOptions.Default, typeof(PackageOverview), exporterOptions);
var supportSchema = JsonSchemaExporter.GetJsonSchemaAsNode(JsonSerializerOptions.Default, typeof(SupportMatrix), exporterOptions);
var reportSchema = JsonSchemaExporter.GetJsonSchemaAsNode(JsonSerializerOptions.Default, typeof(ReportOverview), exporterOptions);
var releaseIndexSchema = JsonSchemaExporter.GetJsonSchemaAsNode(serializerOptions, typeof(ReleaseIndexOverview), exporterOptions);

File.WriteAllText("dotnet-required-packages.json", packageSchema.ToString());
File.WriteAllText("dotnet-support-matrix.json", supportSchema.ToString());
File.WriteAllText("dotnet-support-report.json", reportSchema.ToString());
File.WriteAllText("dotnet-release-index.json", releaseIndexSchema.ToString());


static TAttribute? GetCustomAttribute<TAttribute>(ICustomAttributeProvider? provider, bool inherit = false) where TAttribute : Attribute
    => provider?.GetCustomAttributes(typeof(TAttribute), inherit).FirstOrDefault() as TAttribute;