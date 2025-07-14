using System.ComponentModel;
using System.Reflection;
using DotnetRelease;
using NJsonSchema;
using NJsonSchema.Generation;

List<ModelInfo> models = [
    new (typeof(ReleaseIndex), "major-version-index.json"),
    new (typeof(HistoryIndex), "history-index.json"),
    new (typeof(HistoryYearIndex), "history-year-index.json"),
    new (typeof(HistoryMonthIndex), "history-month-index.json"),
    new (typeof(ReleaseManifest), "release-manifest.json"),
    new (typeof(RuntimeVersionInfo), "runtime-version-info.json"),
    new (typeof(SdkVersionInfo), "sdk-version-info.json"),
    new (typeof(HalLink), "hal-link.json"),
    new (typeof(Support), "support.json"),
    new (typeof(CveRecordSummary), "cve-record-summary.json"),
];

var settings = new SystemTextJsonSchemaGeneratorSettings
{
    FlattenInheritanceHierarchy = true,
    GenerateAbstractProperties = true
};

foreach (var model in models)
{
    await WriteSchemaAsync(model);
}

async Task WriteSchemaAsync(ModelInfo modelInfo)
{
    var (type, targetFile) = modelInfo;
    var schema = JsonSchema.FromType(type, settings);
    
    // Add description from DescriptionAttribute if available
    var descriptionAttribute = type.GetCustomAttribute<DescriptionAttribute>();
    if (descriptionAttribute != null)
    {
        schema.Description = descriptionAttribute.Description;
    }
    
    var schemaJson = schema.ToJson();
    await File.WriteAllTextAsync(targetFile, schemaJson);
    Console.WriteLine($"Generated schema: {targetFile}");
}

record ModelInfo(Type Type, string TargetFile);