using System;
using ApprovalTests;
using NUnit.Framework;

[TestFixture]
public class SagaPropertyMapperTest
{
    [Test]
    public void ShouldThrowForInvlaidPropertyType()
    {
        var exception = Assert.Throws<Exception>(() => SagaPropertyMapper.ExtractProperty<SagaPropertyMapperTest>(test => test.ObjectProperty));
        Approvals.Verify(exception.Message);
    }

    public object ObjectProperty { get; set; }

    [Test]
    public void ExtractProperty()
    {
        var property = SagaPropertyMapper.ExtractProperty<SagaPropertyMapperTest>(test => test.StringProperty);
        Assert.AreEqual("StringProperty", property);
    }

    public string StringProperty { get; set; }
}