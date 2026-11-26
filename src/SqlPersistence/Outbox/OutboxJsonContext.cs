using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(SerializableOperation[]))]
sealed partial class OutboxJsonContext : JsonSerializerContext
{
    // All Unicode ranges stay unescaped to keep the stored text close to what it was, while HTML-sensitive characters are still escaped.
    public static OutboxJsonContext Create() => new(new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    });
}