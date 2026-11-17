using NUnit.Framework;

public class CharArrayTextWriterTests
{
    [Test]
    public void WritingSingleChar()
    {
        var writer = new CharArrayTextWriter();

        writer.Write('z');

        var written = writer.ToString();

        Assert.That(written, Is.EqualTo('z'.ToString()).AsCollection);
    }

    [Test]
    public void WritingCharArray()
    {
        var writer = new CharArrayTextWriter();

        var chars = new[] { 'a', 'b', 'c', 'd', 'e', 'f' };

        const int offset = 5;
        const int take = 1;

        writer.Write(chars, offset, take);
        var written = writer.ToString();

        Assert.That(written, Is.EqualTo(new string(chars, offset, take)));
    }

    [Test]
    public void WritingString()
    {
        var writer = new CharArrayTextWriter();

        writer.Write("test");

        var written = writer.ToString();

        Assert.That(written, Is.EqualTo("test").AsCollection);
    }

    [Test]
    public void WritingCharsBeyondLimit()
    {
        var writer = new CharArrayTextWriter();

        var s = new string('a', CharArrayTextWriter.InitialSize) + "b";

        writer.Write(s);

        var written = writer.ToString();

        Assert.That(written, Is.EqualTo(s).AsCollection);
    }

    [Test]
    public void WritingCharsMuchBeyondLimit()
    {
        var writer = new CharArrayTextWriter();

        var s = new string('a', CharArrayTextWriter.InitialSize * 8);

        writer.Write(s);

        var written = writer.ToString();

        Assert.That(written, Is.EqualTo(s).AsCollection);
    }

    [Test]
    public void HasOffsetResetWhenReleased()
    {
        var writer = CharArrayTextWriter.Lease();
        writer.Write('a');
        writer.Release();

        Assert.That(writer.Size, Is.EqualTo(0));
    }

    [Test]
    public void OversizedWriterIsNotReusedAfterRelease()
    {
        var writer = CharArrayTextWriter.Lease();
        writer.Write(new string('a', CharArrayTextWriter.MaxPooledSize));
        writer.Release();

        Assert.That(CharArrayTextWriter.Lease(), Is.Not.SameAs(writer));
    }
}