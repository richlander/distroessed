using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace EndOfLifeDate;

public class EndOfLifeDate
{
    private const string BaseUrl = "https://deploy-preview-5323--endoflife-date.netlify.app/api/";
    public static Task<IList<SupportCycle>?> GetProduct(HttpClient client, string product) => client.GetFromJsonAsync($"{BaseUrl}{product}.json", SupportCycleSerializerContext.Default.IListSupportCycle);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(IList<SupportCycle>))]
internal partial class SupportCycleSerializerContext : JsonSerializerContext
{
}
