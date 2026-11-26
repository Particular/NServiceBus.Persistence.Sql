using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NServiceBus.Persistence.Sql;

static class JsonPersistenceSerializer
{
    public static string Serialize<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        var buffer = new PooledBufferWriter<byte>(ArrayPool<byte>.Shared);
        try
        {
            using (var writer = new Utf8JsonWriter(buffer, CreateWriterOptions(typeInfo.Options)))
            {
                JsonSerializer.Serialize(writer, value, typeInfo);
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }
        catch (Exception exception)
        {
            throw new SerializationException(exception);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    public static T Deserialize<T>(TextReader reader, JsonTypeInfo<T> typeInfo)
    {
        PooledBufferWriter<char> chars = null;
        PooledBufferWriter<byte> utf8 = null;
        try
        {
            chars = new PooledBufferWriter<char>(ArrayPool<char>.Shared);
            ReadAll(reader, chars);

            // Exact size instead of GetMaxByteCount, which would be three times larger and could cross the pooling limit.
            var utf8Length = Encoding.UTF8.GetByteCount(chars.WrittenSpan);
            utf8 = new PooledBufferWriter<byte>(ArrayPool<byte>.Shared, Math.Max(utf8Length, 1));
            utf8.Advance(Encoding.UTF8.GetBytes(chars.WrittenSpan, utf8.GetSpan(utf8Length)));

            return JsonSerializer.Deserialize(utf8.WrittenSpan, typeInfo);
        }
        catch (Exception exception)
        {
            throw new SerializationException(exception);
        }
        finally
        {
            utf8?.Dispose();
            chars?.Dispose();
        }
    }

    // The array overload is used because the default TextReader.Read(Span<char>) copies through an uncleared shared pool array.
    static void ReadAll(TextReader reader, PooledBufferWriter<char> destination)
    {
        int read;
        do
        {
            var memory = destination.GetMemory();
            MemoryMarshal.TryGetArray<char>(memory, out var segment);
            read = reader.Read(segment.Array, segment.Offset, segment.Count);
            destination.Advance(read);
        } while (read > 0);
    }

    // The writer ignores JsonSerializerOptions, so Encoder and indentation are copied from them to keep both in sync.
    static JsonWriterOptions CreateWriterOptions(JsonSerializerOptions options) => new()
    {
        Encoder = options.Encoder,
        Indented = options.WriteIndented,
        IndentCharacter = options.IndentCharacter,
        IndentSize = options.IndentSize,
        NewLine = options.NewLine,
        MaxDepth = options.MaxDepth
    };
}