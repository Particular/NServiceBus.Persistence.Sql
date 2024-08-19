using System;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

[TestFixture]
public class MessagePropertyMapperTests
{
    [Test]
    public void StringCorrelationId()
    {
        var expression = new SagaWithStringCorrelationId().GetExpression();
        var instance = new SagaWithStringCorrelationId.SagaData
        {
            CorrelationProperty = "Foo"
        };
        var property = expression.Compile()(instance);
        Assert.That(property, Is.EqualTo("Foo"));
    }

    public class SagaWithStringCorrelationId : SqlSaga<SagaWithStringCorrelationId.SagaData>
    {

        public class SagaData : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.CorrelationProperty);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
    }

    [Test]
    public void IntCorrelationId()
    {
        var expression = new SagaWithIntCorrelationId().GetExpression();
        var instance = new SagaWithIntCorrelationId.SagaData
        {
            CorrelationProperty = 10
        };
        var property = expression.Compile()(instance);
        Assert.That(property, Is.EqualTo(10));
    }

    public class SagaWithIntCorrelationId : SqlSaga<SagaWithIntCorrelationId.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public int CorrelationProperty { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.CorrelationProperty);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
    }

    [Test]
    public void GuidCorrelationId()
    {
        var expression = new SagaWithGuidCorrelationId().GetExpression();
        var guid = Guid.NewGuid();
        var instance = new SagaWithGuidCorrelationId.SagaData
        {
            CorrelationProperty = guid
        };
        var property = expression.Compile()(instance);
        Assert.That(property, Is.EqualTo(guid));
    }

    public class SagaWithGuidCorrelationId : SqlSaga<SagaWithGuidCorrelationId.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public Guid CorrelationProperty { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.CorrelationProperty);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
    }
}