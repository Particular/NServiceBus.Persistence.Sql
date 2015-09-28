using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ApprovalTests;
using NServiceBus.Saga;
using NServiceBus.SqlPersistence.Saga;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SagaSerializerTest
{
    [Test]
    public void WithInterface()
    {
        var delegates = SagaXmlSerializerBuiler.BuildSerializationDelegate(typeof(SagaWithInterface));
        var saga = new SagaWithInterface
        {
            Property = "theProperty"
        };

        var xml = Serialize(delegates, saga);
        var result = Deserialize(xml, delegates);
        ObjectApprover.VerifyWithJson(result);
    }

    [Test]
    public void EnsureBasePropsAreNotWrittenWithInterface()
    {
        var delegates = SagaXmlSerializerBuiler.BuildSerializationDelegate(typeof(SagaWithInterface));
        var saga = new SagaWithInterface
        {
            Id = Guid.NewGuid(),
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            Property = "theProperty"
        };
        var xml = Serialize(delegates, saga);
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
        var delegates = SagaXmlSerializerBuiler.BuildSerializationDelegate(typeof(SagaWithAbstract));
        var saga = new SagaWithAbstract
        {
            Property = "PropertyValue"
        };
        var xml = Serialize(delegates, saga);
        var result = Deserialize(xml, delegates);
        ObjectApprover.VerifyWithJson(result);
    }

    [Test]
    public void WithComplexSaga()
    {
        var delegates = SagaXmlSerializerBuiler.BuildSerializationDelegate(typeof(ComplexSaga));
        var saga = new ComplexSaga
        {
            Property = new NestedComplex
            {
                List = new List<string>
                {
                    "listValue"
                },
            }
        };
        var xml = Serialize(delegates, saga);
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
        var delegates = SagaXmlSerializerBuiler.BuildSerializationDelegate(typeof(SagaWithAbstract));
        var saga = new SagaWithAbstract
        {
            Id = Guid.NewGuid(),
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            Property = "theProperty"
        };
        var xml = Serialize(delegates, saga);
        Approvals.Verify(xml);
    }

    public class SagaWithAbstract : ContainSagaData
    {
        public string Property { get; set; }
    }

    static string Serialize(DefualtSerialization delegates, IContainSagaData saga)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            delegates.Serialize(writer, saga);
        }
        return builder.ToString();
    }

    static IContainSagaData Deserialize(string xml, DefualtSerialization delegates)
    {
        var reader = new StringReader(xml);
        var xmlTextReader = new XmlTextReader(reader);
        return delegates.Deserialize(xmlTextReader);
    }
}