using System;
#if NETFRAMEWORK
using System.Runtime.InteropServices;
#endif
using Newtonsoft.Json;

/// <summary>
/// Converts a binary value to and from a base 64 string value.
/// </summary>
class ReadOnlyMemoryConverter : JsonConverter
{
    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        var mem = (ReadOnlyMemory<byte>)value;
        string base64;

#if NETFRAMEWORK
        base64 = MemoryMarshal.TryGetArray(mem, out var bodySegment)
            ? Convert.ToBase64String(bodySegment.Array, bodySegment.Offset, bodySegment.Count)
            : Convert.ToBase64String(mem.ToArray());
#else
        base64 = Convert.ToBase64String(mem.Span);
#endif

        writer.WriteValue(base64);
    }


    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing value of object being read.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <returns>The object value.</returns>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.String)
        {
            // current token is already at base64 string
            // unable to call ReadAsBytes so do it the old fashion way
            string encodedData = reader.Value.ToString();
            byte[] data = Convert.FromBase64String(encodedData);

            var mem = new ReadOnlyMemory<byte>(data);
            return mem;
        }
        else
        {
            throw new Exception($"Unexpected token parsing binary. Expected String or StartArray, got {reader.TokenType}.");
        }
    }

    /// <summary>
    /// Determines whether this instance can convert the specified object type.
    /// </summary>
    /// <param name="objectType">Type of the object.</param>
    /// <returns>
    /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsGenericType
               && objectType.GetGenericTypeDefinition() == typeof(ReadOnlyMemory<>)
               && objectType.GetGenericArguments()[0] == typeof(byte);
    }
}
