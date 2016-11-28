using System.IO;
using Mono.Cecil;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SagaMetaDataReaderTest
{
    ModuleDefinition module;

    public SagaMetaDataReaderTest()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
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
         correlationProperty: nameof(SagaData.Correlation),
         transitionalCorrelationProperty: nameof(SagaData.Transitional)
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

    [Test]
    public void WithNoCorrelation()
    {
        var dataType = module.GetTypeDefinition<WithNoCorrelationSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [SqlSaga]
    public class WithNoCorrelationSaga : Saga<WithNoCorrelationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void WithNoTransitionalCorrelation()
    {
        var dataType = module.GetTypeDefinition<WithNoTransitionalCorrelationSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [SqlSaga]
    public class WithNoTransitionalCorrelationSaga : Saga<WithNoTransitionalCorrelationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

}