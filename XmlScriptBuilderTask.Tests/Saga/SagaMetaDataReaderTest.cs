using System.IO;
using Mono.Cecil;
using NServiceBus;
using NServiceBus.Persistence.Sql.Xml;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SagaMetaDataReaderTest
{
    ModuleDefinition module;

    public SagaMetaDataReaderTest()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "XmlScriptBuilderTask.Tests.dll");
        module = ModuleDefinition.ReadModule(path);
    }

    [Test]
    public void Simple()
    {
        var dataType = module.GetTypeDefinition<SimpleSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [SqlSaga(
         correlationId: nameof(SagaData.Correlation),
         transitionalCorrelationId: nameof(SagaData.Transitional)
     )]
    public class SimpleSaga : Saga<SimpleSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string Transitional { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

}