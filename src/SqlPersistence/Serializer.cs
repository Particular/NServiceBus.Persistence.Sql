using System;
using System.Collections.Generic;
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
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new ReadOnlyMemoryConverter()
            }
        };
        JsonSerializer = JsonSerializer.Create(settings);
    }

    public static T Deserialize<T>(TextReader reader)
    {
        using (var jsonReader = new JsonTextReader(reader))
        {
            try
            {
                return JsonSerializer.Deserialize<T>(jsonReader);
            }
            catch (Exception exception)
            {
                throw new SerializationException(exception);
            }
        }
    }

    public static string Serialize(object target)
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            try
            {
                JsonSerializer.Serialize(jsonWriter, target);
            }
            catch (Exception exception)
            {
                throw new SerializationException(exception);
            }
        }
        return stringBuilder.ToString();
    }
}