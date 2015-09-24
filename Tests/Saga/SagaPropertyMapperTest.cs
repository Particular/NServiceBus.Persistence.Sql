using NUnit.Framework;

[TestFixture]
public class SagaPropertyMapperTest
{
    [Test]
    public void TryGetBaseSagaType()
    {
        SagaPropertyMapper.ExtractProperty<SagaPropertyMapperTest>(test => test.Property);
    }

    public object Property { get; set; }
}