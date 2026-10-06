using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class MsSqlServerOutboxPaddingTests
{
    [TestCase(0)]
    [TestCase(100)]
    [TestCase(2000)]
    [TestCase(2001)]
    [TestCase(4000)]
    public void Pads_short_content_past_8000_bytes(int length)
    {
        var dialect = new SqlDialect.MsSqlServer();
        var json = new string('a', length);

        var padded = dialect.AddOutboxPadding(json);

        Assert.Multiple(() =>
        {
            // nvarchar is UTF-16, so 4001 chars is the first length over 8000 bytes
            Assert.That(padded, Has.Length.EqualTo(4001));
            Assert.That(padded, Does.StartWith(json));
            Assert.That(padded[length..].Trim(), Is.Empty);
        });
    }

    [TestCase(4001)]
    [TestCase(10000)]
    public void Leaves_long_content_unchanged(int length)
    {
        var dialect = new SqlDialect.MsSqlServer();
        var json = new string('a', length);

        var padded = dialect.AddOutboxPadding(json);

        Assert.That(padded, Is.EqualTo(json));
    }
}
