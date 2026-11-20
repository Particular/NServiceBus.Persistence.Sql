using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;

public class JsonArrayPoolTests
{
    [TestCase(1, true)]
    [TestCase(JsonArrayPool.MaxPooledSize, true)]
    [TestCase(JsonArrayPool.MaxPooledSize + 1, false)]
    public void ReturnsOnlyArraysUpToTheLimit(int minimumLength, bool expectedToBeReturned)
    {
        var inner = new RecordingPool();
        var pool = new JsonArrayPool(inner);

        var array = pool.Rent(minimumLength);
        pool.Return(array);

        Assert.That(inner.Returned.Contains(array), Is.EqualTo(expectedToBeReturned));
    }

    [Test]
    public void ReturnedArraysAreCleared()
    {
        var inner = new RecordingPool();
        var pool = new JsonArrayPool(inner);

        pool.Return(pool.Rent(16));

        Assert.That(inner.ClearedOnReturn, Is.EqualTo(new[] { true }));
    }

    [Test]
    public void ReturningNullIsIgnored()
    {
        var inner = new RecordingPool();

        new JsonArrayPool(inner).Return(null);

        Assert.That(inner.Returned, Is.Empty);
    }

    [Test]
    public void ReaderReturnsItsBufferWhenParsingFails()
    {
        var inner = new RecordingPool();
        var reader = new JsonTextReader(new StringReader("{\"Value\": ")) { ArrayPool = new JsonArrayPool(inner) };

        Assert.Catch<JsonException>(() =>
        {
            using (reader)
            {
                Serializer.JsonSerializer.Deserialize<Payload>(reader);
            }
        });

        Assert.That(inner.Returned, Is.Not.Empty);
    }

    [TestCase(100)]
    [TestCase(JsonArrayPool.MaxPooledSize * 2)]
    public void LargeStringsWithEscapesRoundTrip(int length)
    {
        var value = new string('a', length - 2) + "\"\n";

        var json = Serializer.Serialize(new Payload { Value = value });
        var result = Serializer.Deserialize<Payload>(new StringReader(json));

        Assert.That(result.Value, Is.EqualTo(value));
    }

    public class Payload
    {
        public string Value { get; set; }
    }

    sealed class RecordingPool : ArrayPool<char>
    {
        public List<char[]> Returned { get; } = [];
        public List<bool> ClearedOnReturn { get; } = [];

        public override char[] Rent(int minimumLength) => new char[minimumLength];

        public override void Return(char[] array, bool clearArray = false)
        {
            Returned.Add(array);
            ClearedOnReturn.Add(clearArray);
        }
    }
}
