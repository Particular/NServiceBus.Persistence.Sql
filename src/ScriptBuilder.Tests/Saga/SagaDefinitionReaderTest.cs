using System;
using System.IO;
using Mono.Cecil;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class SagaDefinitionReaderTest : IDisposable
{
    ModuleDefinition module;

    public SagaDefinitionReaderTest()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
        var readerParameters = new ReaderParameters(ReadingMode.Deferred);
        module = ModuleDefinition.ReadModule(path, readerParameters);
    }

    [Test]
    public void WithGeneric()
    {
        var sagaType = module.GetTypeDefinition<WithGenericSaga<int>>();
        var exception = Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinitionReader.TryGetSagaDefinition(sagaType, out _);
        });
        Assert.That(exception.Message, Is.Not.Null);
        Approver.Verify(exception.Message);
    }

    public class WithGenericSaga<T> : Saga<WithGenericSaga<T>.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => throw new NotImplementedException();
    }


    [Test]
    public void Abstract()
    {
        var sagaType = module.GetTypeDefinition<AbstractSaga>();
        var exception = Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinitionReader.TryGetSagaDefinition(sagaType, out _);
        });
        Assert.That(exception.Message, Is.Not.Null);
        Approver.Verify(exception.Message);
    }

    abstract class AbstractSaga : Saga<AbstractSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }
    }

    [Test]
    public void Simple()
    {
        var sagaType = module.GetTypeDefinition<SimpleSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(sagaType, out var definition);

        Assert.That(definition, Is.Not.Null);
        Approver.Verify(definition);
    }

    [SqlSaga(correlationProperty: nameof(SagaData.Correlation), transitionalCorrelationProperty: nameof(SagaData.Transitional))]
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
    public void WithNoTransitionalCorrelation()
    {
        var sagaType = module.GetTypeDefinition<WithNoTransitionalCorrelationSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(sagaType, out var definition);
        Assert.That(definition, Is.Not.Null);
        Approver.Verify(definition);
    }

    [SqlSaga(correlationProperty: nameof(SagaData.Correlation))]
    public class WithNoTransitionalCorrelationSaga : Saga<WithNoTransitionalCorrelationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => throw new NotImplementedException();
    }

    [Test]
    public void WithTableSuffix()
    {
        var sagaType = module.GetTypeDefinition<TableSuffixSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(sagaType, out var definition);
        Assert.That(definition, Is.Not.Null);
        Approver.Verify(definition);
    }

    [SqlSaga(tableSuffix: "TheTableSuffix", correlationProperty: nameof(SagaData.Correlation))]
    public class TableSuffixSaga : Saga<TableSuffixSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => throw new NotImplementedException();
    }

    [Test]
    public void WithNoCorrelation()
    {
        var sagaType = module.GetTypeDefinition<WithNoCorrelationSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(sagaType, out var definition);
        Assert.That(definition, Is.Not.Null);
        Approver.Verify(definition);
    }

    public class WithNoCorrelationSaga : Saga<WithNoCorrelationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => throw new NotImplementedException();
    }

    [Test]
    public void CorrelationIdInBaseClassSqlSaga()
    {
        var sagaType = module.GetTypeDefinition<SqlSagaWithDataBaseClass>();
        SagaDefinitionReader.TryGetSagaDefinition(sagaType, out var definition);
        Assert.That(definition, Is.Not.Null);
        Approver.Verify(definition);
    }

    [SqlSaga(tableSuffix: "TheTableSuffix", correlationProperty: nameof(SagaData.MyId))]
    public class SqlSagaWithDataBaseClass : Saga<SqlSagaWithDataBaseClass.SagaData>
    {
        public abstract class SagaDataBaseClass : ContainSagaData
        {
            public string MyId { get; set; }
        }

        public class SagaData : SagaDataBaseClass
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => throw new NotImplementedException();
    }

    [Test]
    public void CorrelationIdInTwoLevelsDeepBaseClassSqlSaga()
    {
        var sagaType = module.GetTypeDefinition<SqlSagaWithTwoLevelsDeepDataBaseClass>();
        SagaDefinitionReader.TryGetSagaDefinition(sagaType, out var definition);
        Assert.That(definition, Is.Not.Null);
        Approver.Verify(definition);
    }

    [SqlSaga(tableSuffix: "TheTableSuffix", correlationProperty: nameof(SagaData.MyId))]
    public class SqlSagaWithTwoLevelsDeepDataBaseClass : Saga<SqlSagaWithTwoLevelsDeepDataBaseClass.SagaData>
    {
        public abstract class SagaDataBaseClassOne : ContainSagaData
        {
            public abstract string MyId { get; set; }
        }

        public class SagaDataBaseClassTwo : SagaDataBaseClassOne
        {
            public override string MyId { get; set; }
        }

        public class SagaData : SagaDataBaseClassTwo
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => throw new NotImplementedException();
    }

    [Test]
    public void CorrelationIdInBaseClassRegularSaga()
    {
        var sagaType = module.GetTypeDefinition<SagaWithDataBaseClass>();
        SagaDefinitionReader.TryGetSagaDefinition(sagaType, out var definition);
        Assert.That(definition, Is.Not.Null);
        Approver.Verify(definition);
    }

    [SqlSaga(tableSuffix: "TheTableSuffix")]
    public class SagaWithDataBaseClass : Saga<SagaWithDataBaseClass.SagaData>
    {
        public abstract class SagaDataBaseClass : ContainSagaData
        {
            public string MyId { get; set; }
        }

        public class SagaData : SagaDataBaseClass
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.ConfigureMapping<AMessage>(m => m.MyId).ToSaga(s => s.MyId);
    }

    class AMessage
    {
        public Guid MyId { get; set; }
    }

    public void Dispose()
    {
        module?.Dispose();
    }
}