using System.Text.Json;
using System.Text.Json.Serialization;

namespace EndOfLifeDate;

public class EolStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
    {
        JsonTokenType.True => "True",
        JsonTokenType.False => "False",
        _ => reader.GetString()
    };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
