using System;
using System.Linq.Expressions;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

[TestFixture]
public class MessagePropertyMapperTests
{

    [Test]
    public void StringCorrelationId()
    {
        Expression<Func<SagaWithStringCorrelationId.SagaData, object>> expression;
        Assert.IsTrue(MessagePropertyMapper<SagaWithStringCorrelationId.SagaData>.TryGetExpression(typeof(SagaWithStringCorrelationId), out expression));
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
        Expression<Func<SagaWithIntCorrelationId.SagaData, object>> expression;
        Assert.IsTrue(MessagePropertyMapper<SagaWithIntCorrelationId.SagaData>.TryGetExpression(typeof(SagaWithIntCorrelationId), out expression));
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
        Expression<Func<SagaWithGuidCorrelationId.SagaData, object>> expression;
        Assert.IsTrue(MessagePropertyMapper<SagaWithGuidCorrelationId.SagaData>.TryGetExpression(typeof(SagaWithGuidCorrelationId), out expression));
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