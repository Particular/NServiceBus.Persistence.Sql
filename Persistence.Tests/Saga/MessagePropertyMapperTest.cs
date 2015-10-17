using System;
using System.Diagnostics;
using System.Linq.Expressions;
using NServiceBus.SqlPersistence;
using NUnit.Framework;

[TestFixture]
public class MessagePropertyMapperTest
{

    [Test]
    public void Complete()
    {
        Expression<Func<SagaDataWithCorrelationId, object>> expression;
        Assert.IsTrue(MessagePropertyMapper<SagaDataWithCorrelationId>.TryGetExpression(out expression));
        var instance = new SagaDataWithCorrelationId
        {
            CorrelationProperty = "Foo"
        };
        var property = expression.Compile()(instance);
        Assert.AreEqual("Foo",property);
    }

    public class SagaDataWithCorrelationId : XmlSagaData
    {
        [CorrelationId]
        public string CorrelationProperty { get; set; }
    }
    public class MySaga : XmlSaga<SagaData>
    {
        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
            mapper.MapMessage<Message>(message => message.CorrelationProperty);
        }
    }

    public class SagaData : XmlSagaData
    {
        [CorrelationId]
        public string CorrelationProperty { get; set; }
    }
    public class Message 
    {
        public string CorrelationProperty { get; set; }
    }
}