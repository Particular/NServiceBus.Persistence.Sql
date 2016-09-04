using System.IO;
using Mono.Cecil;
using NServiceBus.Persistence.Sql.Xml;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class CorrelationReaderTest
{
    ModuleDefinition module;

    public CorrelationReaderTest()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "XmlScriptBuilderTask.Tests.dll");
        module = ModuleDefinition.ReadModule(path);
    }

    [Test]
    public void WithValidProperty()
    {
        var dataType = module.GetTypeDefinition<DataWithValidProperty>();
        var correlationMember = CorrelationReader.GetCorrelationMember(dataType);
        ObjectApprover.VerifyWithJson(correlationMember);
    }

    public class DataWithValidProperty
    {
        [CorrelationId]
        public string Correlation { get; set; }
        [TransitionalCorrelationId]
        public string TransitionalCorrelation { get; set; }
    }


    [Test]
    public void WithNone()
    {
        var dataType = module.GetTypeDefinition<DataWithNone>();
        var correlationMember = CorrelationReader.GetCorrelationMember(dataType);
        ObjectApprover.VerifyWithJson(correlationMember);
    }

    public class DataWithNone
    {
        public string Property{ get; set; }
    }

    [Test]
    public void ErrorForMultiple()
    {
        var dataType = module.GetTypeDefinition<DataWithMultiple>();
        var exception = Assert.Throws<ErrorsException>(() => CorrelationReader.GetCorrelationMember(dataType));
        ObjectApprover.VerifyWithJson(exception.Message);
    }

    public class DataWithMultiple
    {
        [CorrelationId]
        public string Correlation1 { get; set; }
        [CorrelationId]
        public string Correlation2 { get; set; }
    }

}