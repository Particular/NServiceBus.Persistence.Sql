using System.IO;
using System.Text;
using Newtonsoft.Json;

static class Serializer
{
    static JsonSerializer jsonSerializer;
    static Serializer()
    {
        jsonSerializer = JsonSerializer.CreateDefault();
    }
    public static T Deserialize<T>(TextReader textReader)
    {
        using (var jsonReader = new JsonTextReader(textReader))
        {
            return jsonSerializer.Deserialize<T>(jsonReader);
        }
    }

    public static string Serialize(object target)
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonSerializer.Serialize(jsonWriter, target);
        }
        return stringBuilder.ToString();
    }
}