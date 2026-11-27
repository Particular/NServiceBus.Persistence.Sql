using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

public class JsonPersistenceSerializerTests
{
    const int ChunkSize = 4096;
    const string Emoji = "😀";

    static readonly JsonTypeInfo<string> TypeInfo =
        (JsonTypeInfo<string>)new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() }.GetTypeInfo(typeof(string));

    public enum ReaderKind
    {
        Whole,
        OneCharacterPerRead,
        FirstReadOf4095Characters
    }

    [Test]
    public void SurrogatePairSplitAcrossTheChunkBoundaryRoundTrips(
        [Values(ChunkSize - 2, ChunkSize - 1)] int highSurrogateIndex,
        [Values] ReaderKind readerKind)
    {
        var value = new string('a', highSurrogateIndex - 1) + Emoji + "tail";
        var json = Quote(value);
        Assert.That(json[highSurrogateIndex], Is.EqualTo(Emoji[0]));

        var result = JsonPersistenceSerializer.Deserialize(CreateReader(json, readerKind), TypeInfo);

        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void LoneHighSurrogateAtTheChunkBoundaryBecomesReplacementCharacter([Values] ReaderKind readerKind)
    {
        var json = Quote(new string('a', ChunkSize - 2) + Emoji[0] + "tail");

        var result = JsonPersistenceSerializer.Deserialize(CreateReader(json, readerKind), TypeInfo);

        Assert.That(result, Is.EqualTo(new string('a', ChunkSize - 2) + "\uFFFDtail"));
    }

    [Test]
    public void LoneHighSurrogateAtTheEndOfTheInputIsReplacedAndRejectedAsInvalidJson([Values] ReaderKind readerKind)
    {
        var json = Quote("value") + Emoji[0];

        var exception = Assert.Throws<SerializationException>(() => JsonPersistenceSerializer.Deserialize(CreateReader(json, readerKind), TypeInfo));

        Assert.That(exception.InnerException, Is.InstanceOf<JsonException>());
    }

    [Test]
    public void DocumentLargerThanOneMegabyteRoundTrips([Values] ReaderKind readerKind)
    {
        var value = string.Concat(Enumerable.Repeat("abc ü € " + Emoji, 150_000));
        var json = Quote(value);
        Assert.That(json.Length, Is.GreaterThan(1024 * 1024));

        var result = JsonPersistenceSerializer.Deserialize(CreateReader(json, readerKind), TypeInfo);

        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void EmptyReaderThrowsSerializationException() =>
        Assert.Throws<SerializationException>(() => JsonPersistenceSerializer.Deserialize(new StringReader(""), TypeInfo));

    static string Quote(string value) => "\"" + value + "\"";

    static TextReader CreateReader(string text, ReaderKind kind) => kind switch
    {
        ReaderKind.Whole => new StringReader(text),
        ReaderKind.OneCharacterPerRead => new LimitedReader(text, 1, 1),
        ReaderKind.FirstReadOf4095Characters => new LimitedReader(text, ChunkSize - 1, int.MaxValue),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    sealed class LimitedReader(string text, int firstRead, int laterReads) : TextReader
    {
        int position;
        bool first = true;

        public override int Read(char[] buffer, int index, int count)
        {
            var limit = first ? firstRead : laterReads;
            first = false;
            var length = Math.Min(Math.Min(count, limit), text.Length - position);
            text.CopyTo(position, buffer, index, length);
            position += length;
            return length;
        }
    }
}
