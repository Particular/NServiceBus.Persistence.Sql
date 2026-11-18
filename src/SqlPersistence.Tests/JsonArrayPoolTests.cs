using System.IO;
using NUnit.Framework;

public class JsonArrayPoolTests
{
    [TestCase(1)]
    [TestCase(1025)]
    [TestCase(JsonArrayPool.MaxPooledSize + 1)]
    public void RentedArraysCanBeReturned(int minimumLength)
    {
        var array = JsonArrayPool.Instance.Rent(minimumLength);

        Assert.That(array.Length, Is.GreaterThanOrEqualTo(minimumLength));
        Assert.DoesNotThrow(() => JsonArrayPool.Instance.Return(array));
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
}
