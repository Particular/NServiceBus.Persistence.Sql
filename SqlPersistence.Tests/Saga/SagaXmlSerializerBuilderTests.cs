using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ApprovalTests;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SagaXmlSerializerBuilderTests
{


    [Test]
    public void WithSimple()
    {
        var delegates = SagaXmlSerializerBuilder.BuildSerializationDelegate(typeof(SimpleSagaData));
        var saga = new SimpleSagaData
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
        var delegates = SagaXmlSerializerBuilder.BuildSerializationDelegate(typeof(ComplexSaga));
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
    public void EnsureBasePropsAreNotWritten()
    {
        var delegates = SagaXmlSerializerBuilder.BuildSerializationDelegate(typeof(SimpleInheritingFromContainSagaData));
        var saga = new SimpleInheritingFromContainSagaData
        {
            Id = Guid.NewGuid(),
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            Property = "theProperty"
        };
        var xml = Serialize(delegates, saga);
        Approvals.Verify(xml);
    }

    [Test]
    public void EnsureBasePropsAreNotWrittenCustom()
    {
        var delegates = SagaXmlSerializerBuilder.BuildSerializationDelegate(typeof(SimpleInheritingFromIContainSagaData));
        var saga = new SimpleInheritingFromIContainSagaData
        {
            Id = Guid.NewGuid(),
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            Property = "theProperty"
        };
        var xml = Serialize(delegates, saga);
        Approvals.Verify(xml);
    }

    public class SimpleSagaData : ContainSagaData
    {
        public string Property { get; set; }
    }

    public class SimpleInheritingFromContainSagaData : ContainSagaData
    {
        public string Property { get; set; }
    }

    public class SimpleInheritingFromIContainSagaData : IContainSagaData
    {
        public string Property { get; set; }
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }

    static string Serialize(DefaultSagaSerialization delegates, IContainSagaData saga)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            delegates.Serialize(writer, saga);
        }
        return builder.ToString();
    }

    static IContainSagaData Deserialize(string xml, DefaultSagaSerialization delegates)
    {
        var reader = new StringReader(xml);
        var xmlTextReader = new XmlTextReader(reader);
        return delegates.Deserialize(xmlTextReader);
    }
}