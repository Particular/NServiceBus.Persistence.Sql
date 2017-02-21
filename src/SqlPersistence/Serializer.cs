using System.IO;
using System.Text;
using Newtonsoft.Json;

static class Serializer
{
    public static JsonSerializer JsonSerializer;

    static Serializer()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
        JsonSerializer = JsonSerializer.Create(settings);
    }

    public static T Deserialize<T>(TextReader reader)
    {
        using (var jsonReader = new JsonTextReader(reader))
        {
            return JsonSerializer.Deserialize<T>(jsonReader);
        }
    }

    public static string Serialize(object target)
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            JsonSerializer.Serialize(jsonWriter, target);
        }
        return stringBuilder.ToString();
    }
}