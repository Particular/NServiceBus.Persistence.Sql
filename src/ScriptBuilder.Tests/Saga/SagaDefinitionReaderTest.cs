using System.IO;
using Mono.Cecil;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SagaDefinitionReaderTest
{
    ModuleDefinition module;

    public SagaDefinitionReaderTest()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
        var readerParameters = new ReaderParameters(ReadingMode.Deferred);
        module = ModuleDefinition.ReadModule(path, readerParameters);
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
        correlationProperty : nameof(SagaData.Correlation),
        transitionalCorrelationProperty : nameof(SagaData.Transitional)
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
    public void SqlSaga()
    {
        var dataType = module.GetTypeDefinition<SimpleSqlSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [SqlSaga(
        correlationProperty: nameof(SagaData.Correlation),
        transitionalCorrelationProperty: nameof(SagaData.Transitional)
    )]
    public class SimpleSqlSaga : SqlSaga<SimpleSqlSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string Transitional { get; set; }
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
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

    [Test]
    public void WithTableSuffix()
    {
        var dataType = module.GetTypeDefinition<TableSuffixSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [SqlSaga(
        correlationProperty: nameof(SagaData.Correlation),
        tableSuffix: "TheTableSuffix"
    )]
    public class TableSuffixSaga : Saga<TableSuffixSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }
}