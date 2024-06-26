using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace EndOfLifeDate;

public class Product
{
    private const string BaseUrl = "https://deploy-preview-5323--endoflife-date.netlify.app/api/";
    public static Task<IList<SupportCycle>?> GetProduct(HttpClient client, string product) => client.GetFromJsonAsync($"{BaseUrl}{product}.json", SupportCycleSerializerContext.Default.IListSupportCycle);
    public static Task<SupportCycle?> GetProductCycle(HttpClient client, string product, string cycle) => client.GetFromJsonAsync($"{BaseUrl}{product}/{cycle}.json", SupportCycleSerializerContext.Default.SupportCycle);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(IList<SupportCycle>))]
[JsonSerializable(typeof(SupportCycle))]
internal partial class SupportCycleSerializerContext : JsonSerializerContext
{
}
