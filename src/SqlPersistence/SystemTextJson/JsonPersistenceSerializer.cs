using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.Unicode;
using NServiceBus.Persistence.Sql;

static class JsonPersistenceSerializer
{
    const int ChunkSize = 4096;
    const int MaxUtf8BytesPerChar = 3;

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
        var chunk = ArrayPool<char>.Shared.Rent(ChunkSize);
        PooledBufferWriter<byte> utf8 = null;
        try
        {
            utf8 = new PooledBufferWriter<byte>(ArrayPool<byte>.Shared);
            Transcode(reader, chunk, utf8);

            return JsonSerializer.Deserialize(utf8.WrittenSpan, typeInfo);
        }
        catch (Exception exception)
        {
            throw new SerializationException(exception);
        }
        finally
        {
            utf8?.Dispose();
            ArrayPool<char>.Shared.Return(chunk, clearArray: true);
        }
    }

    static void Transcode(TextReader reader, char[] chunk, PooledBufferWriter<byte> utf8)
    {
        var carried = 0;
        while (true)
        {
            // The array overload is used because the default Read(Span<char>) copies through an uncleared shared pool array.
            var read = reader.Read(chunk, carried, ChunkSize - carried);
            var isFinalBlock = read == 0;
            var source = chunk.AsSpan(0, carried + read);

            var status = Utf8.FromUtf16(source, utf8.GetSpan(source.Length * MaxUtf8BytesPerChar), out var charsRead, out var bytesWritten, replaceInvalidSequences: true, isFinalBlock);
            utf8.Advance(bytesWritten);

            var completed = status switch
            {
                OperationStatus.Done => isFinalBlock,
                OperationStatus.NeedMoreData when !isFinalBlock => false,
                OperationStatus.NeedMoreData or OperationStatus.DestinationTooSmall or OperationStatus.InvalidData =>
                    throw new InvalidOperationException($"Unexpected transcoding status {status}."),
                _ => throw new InvalidOperationException($"Unknown transcoding status {status}.")
            };

            if (completed)
            {
                return;
            }

            carried = source.Length - charsRead;
            source[charsRead..].CopyTo(chunk);
        }
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