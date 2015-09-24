using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using ApprovalTests;
using NServiceBus.Saga;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SagaSerializerTest
{
    [Test]
    public void WithInterface()
    {
        var xml = SagaSerializer.ToXml(new SagaWithInterface
        {
            Property = "theProperty"
        });
        var result = Deserialize<SagaWithInterface>(xml);
        ObjectApprover.VerifyWithJson(result);
    }

    [Test]
    public void EnsureBasePropsAreNotWrittenWithInterface()
    {
        var xml = SagaSerializer.ToXml(new SagaWithInterface
        {
            Id = Guid.NewGuid(),
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            Property = "theProperty"
        });
        Approvals.Verify(xml);
    }

    public class SagaWithInterface : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public string Property { get; set; }
    }

    [Test]
    public void WithAbstract()
    {
        var xml = SagaSerializer.ToXml(new SagaWithAbstract
        {
            Property = "PropertyValue"
        });
        var result = Deserialize<SagaWithAbstract>(xml);
        ObjectApprover.VerifyWithJson(result);
    }

    [Test]
    public void WithComplexSaga()
    {
        var xml = SagaSerializer.ToXml(new ComplexSaga
        {
            Property = new NestedComplex
            {
                List = new List<string>
                {
                    "listValue"
                },
            }
        });
        Approvals.Verify(xml);
    }

    public class ComplexSaga : ContainSagaData
    {
        public NestedComplex Property { get; set; }
    }

    public class NestedComplex
    {
        public List<string> List { get; set; }
    }

    [Test]
    public void EnsureBasePropsAreNotWrittenWithAbstract()
    {
        var xml = SagaSerializer.ToXml(new SagaWithAbstract
        {
            Id = Guid.NewGuid(),
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            Property = "theProperty"
        });
        Approvals.Verify(xml);
    }

    public class SagaWithAbstract : ContainSagaData
    {
        public string Property { get; set; }
    }

    static T Deserialize<T>(string xml) where T : IContainSagaData
    {
        return SagaSerializer.FromString<T>(new XmlTextReader(new StringReader(xml)));
    }
}