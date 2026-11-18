using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

public class PooledCharTextWriterTests
{
    [Test]
    public void WritingSingleChar()
    {
        var writer = new PooledCharTextWriter();

        writer.Write('z');

        Assert.That(writer.ToString(), Is.EqualTo("z"));
        writer.ReturnBuffer();
    }

    [Test]
    public void WritingCharArray()
    {
        var writer = new PooledCharTextWriter();
        var chars = new[] { 'a', 'b', 'c', 'd', 'e', 'f' };

        writer.Write(chars, 5, 1);

        Assert.That(writer.ToString(), Is.EqualTo("f"));
        writer.ReturnBuffer();
    }

    [Test]
    public void WritingString()
    {
        var writer = new PooledCharTextWriter();

        writer.Write("test");
        writer.Write((string)null);

        Assert.That(writer.ToString(), Is.EqualTo("test"));
        writer.ReturnBuffer();
    }

    [TestCase(PooledCharTextWriter.InitialSize + 1)]
    [TestCase(PooledCharTextWriter.InitialSize * 8)]
    [TestCase(PooledCharTextWriter.MaxPooledSize + 1)]
    public void WritingBeyondCapacity(int size)
    {
        var writer = new PooledCharTextWriter();
        var s = new string('a', size - 1) + "b";

        foreach (var chunk in s.Chunk(1000))
        {
            writer.Write(chunk);
        }

        Assert.That(writer.ToString(), Is.EqualTo(s));
        writer.ReturnBuffer();
    }

    [Test]
    public void ReturningBufferResetsContent()
    {
        var writer = new PooledCharTextWriter();
        writer.Write("data");

        writer.ReturnBuffer();

        Assert.That(writer.ToString(), Is.Empty);
    }

    [Test]
    public void ContentSurvivesJsonWriterClosingTheWriter()
    {
        var writer = new PooledCharTextWriter();

        using (var jsonWriter = new JsonTextWriter(writer))
        {
            jsonWriter.WriteValue("value");
        }

        Assert.That(writer.ToString(), Is.EqualTo("\"value\""));
        writer.ReturnBuffer();
    }
}
