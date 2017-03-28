using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using NServiceBus.Persistence.Sql;


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
        try
        {
            using (var jsonReader = new JsonTextReader(reader))
            {
                return JsonSerializer.Deserialize<T>(jsonReader);
            }
        }
        catch (Exception exception)
        {
            throw new SerializationException(exception);
        }
    }

    public static string Serialize(object target)
    {
        try
        {
            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                JsonSerializer.Serialize(jsonWriter, target);
            }
            return stringBuilder.ToString();
        }
        catch (Exception exception)
        {
            throw new SerializationException(exception);
        }
    }
}