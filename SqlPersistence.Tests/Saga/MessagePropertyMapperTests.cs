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
        var expression = MessagePropertyMapper<SagaWithStringCorrelationId.SagaData>.GetExpression(typeof(SagaWithStringCorrelationId));;
        var instance = new SagaWithStringCorrelationId.SagaData
        {
            CorrelationProperty = "Foo"
        };
        var property = expression.Compile()(instance);
        Assert.AreEqual("Foo", property);
    }

    [SqlSaga("CorrelationProperty")]
    public class SagaWithStringCorrelationId
    {

        public class SagaData : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
        }
    }

    [Test]
    public void IntCorrelationId()
    {
        var expression = MessagePropertyMapper<SagaWithIntCorrelationId.SagaData>.GetExpression(typeof(SagaWithIntCorrelationId));
        var instance = new SagaWithIntCorrelationId.SagaData
        {
            CorrelationProperty = 10
        };
        var property = expression.Compile()(instance);
        Assert.AreEqual(10, property);
    }

    [SqlSaga("CorrelationProperty")]
    public class SagaWithIntCorrelationId
    {
        public class SagaData : ContainSagaData
        {
            public int CorrelationProperty { get; set; }
        }
    }

    [Test]
    public void GuidCorrelationId()
    {
        var expression = MessagePropertyMapper<SagaWithGuidCorrelationId.SagaData>.GetExpression(typeof(SagaWithGuidCorrelationId));
        var guid = Guid.NewGuid();
        var instance = new SagaWithGuidCorrelationId.SagaData
        {
            CorrelationProperty = guid
        };
        var property = expression.Compile()(instance);
        Assert.AreEqual(guid, property);
    }

    [SqlSaga("CorrelationProperty")]
    public class SagaWithGuidCorrelationId
    {
        public class SagaData : ContainSagaData
        {
            public Guid CorrelationProperty { get; set; }
        }
    }

}