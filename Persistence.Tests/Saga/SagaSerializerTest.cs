using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ApprovalTests;
using NServiceBus;
using NServiceBus.SqlPersistence;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SagaSerializerTest
{


    [Test]
    public void WithSimple()
    {
        var delegates = SagaXmlSerializerBuilder.BuildSerializationDelegate(typeof(SimpleSaga), (serializer, type) => { });
        var saga = new SimpleSaga
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
        var delegates = SagaXmlSerializerBuilder.BuildSerializationDelegate(typeof(ComplexSaga), (serializer, type) => { });
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

    public class ComplexSaga : XmlSagaData
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
        var delegates = SagaXmlSerializerBuilder.BuildSerializationDelegate(typeof(SimpleSaga), (serializer, type) => { });
        var saga = new SimpleSaga
        {
            Id = Guid.NewGuid(),
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            Property = "theProperty"
        };
        var xml = Serialize(delegates, saga);
        Approvals.Verify(xml);
    }

    public class SimpleSaga : XmlSagaData
    {
        public string Property { get; set; }
    }

    static string Serialize(DefaultSagaSerialization delegates, XmlSagaData saga)
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