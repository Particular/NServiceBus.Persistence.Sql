using System.Text.Json;
using System.Text.Json.Serialization;

// Metadata only, because a custom encoder rules out the generated fast path; options come from Create().
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(SerializableOperation[]))]
sealed partial class OutboxJsonContext : JsonSerializerContext
{
    // Non-ASCII characters stay raw except the ones Newtonsoft escapes; HTML-sensitive ASCII is still escaped.
    public static OutboxJsonContext Create() => new(new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true,
        Encoder = NonAsciiPreservingJavaScriptEncoder.Instance
    });
}