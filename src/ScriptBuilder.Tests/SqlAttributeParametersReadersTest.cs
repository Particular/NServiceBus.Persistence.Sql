using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class SqlAttributeParametersReadersTest
{
    [Test]
    public void Defaults()
    {
        var result = SettingsAttributeReader.ReadFromAttribute(null);
        Assert.That(result, Is.Not.Null);
        Approver.Verify(result);
    }


}