using System;
using System.IO;
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
            Converters = [new ReadOnlyMemoryConverter()]
        };
        JsonSerializer = JsonSerializer.Create(settings);
    }

    public static T Deserialize<T>(TextReader reader)
    {
        using (var jsonReader = new JsonTextReader(reader) { ArrayPool = JsonArrayPool.Instance })
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
        var writer = new PooledCharTextWriter();
        try
        {
            using (var jsonWriter = new JsonTextWriter(writer))
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

            return writer.ToString();
        }
        finally
        {
            writer.ReturnBuffer();
        }
    }
}