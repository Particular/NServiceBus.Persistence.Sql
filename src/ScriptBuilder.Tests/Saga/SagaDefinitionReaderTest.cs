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

    public class WithGenericSaga<T> : SqlSaga<WithGenericSaga<T>.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.Correlation);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
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

    abstract class AbstractSaga : SqlSaga<AbstractSaga.SagaData>
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

    public class SimpleSaga : SqlSaga<SimpleSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string Transitional { get; set; }
        }

        protected override string TransitionalCorrelationPropertyName => nameof(SagaData.Transitional);

        protected override string CorrelationPropertyName => nameof(SagaData.Correlation);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
    }

    [Test]
    public void WithReadonlyProperty()
    {
        var sagaType = module.GetTypeDefinition<WithReadonlyPropertySaga>();
        var exception = Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinitionReader.TryGetSagaDefinition(sagaType, out _);
        });
        Assert.That(exception.Message, Is.Not.Null);
        Approver.Verify(exception.Message);
    }

    public class WithReadonlyPropertySaga : SqlSaga<WithReadonlyPropertySaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.Correlation);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
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

    public class WithNoTransitionalCorrelationSaga : SqlSaga<WithNoTransitionalCorrelationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.Correlation);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
    }

    [Test]
    public void WithTableSuffix()
    {
        var sagaType = module.GetTypeDefinition<TableSuffixSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(sagaType, out var definition);
        Assert.That(definition, Is.Not.Null);
        Approver.Verify(definition);
    }

    public class TableSuffixSaga : SqlSaga<TableSuffixSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override string TableSuffix => "TheTableSuffix";
        protected override string CorrelationPropertyName => nameof(SagaData.Correlation);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
    }

    [Test]
    public void WithNoCorrelation()
    {
        var sagaType = module.GetTypeDefinition<WithNoCorrelationSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(sagaType, out var definition);
        Assert.That(definition, Is.Not.Null);
        Approver.Verify(definition);
    }

    public class WithNoCorrelationSaga : SqlSaga<WithNoCorrelationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override string CorrelationPropertyName => null;

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
    }

    [Test]
    public void WithStatementBodyProperty()
    {
        var sagaType = module.GetTypeDefinition<WithStatementBodyPropertySaga>();
        SagaDefinitionReader.TryGetSagaDefinition(sagaType, out var definition);
        Assert.That(definition, Is.Not.Null);
        Approver.Verify(definition);
    }

    public class WithStatementBodyPropertySaga : SqlSaga<WithStatementBodyPropertySaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
        }

        protected override string CorrelationPropertyName
        {
            //Explicitly not use expression body
#pragma warning disable IDE0025 // Use expression body for properties
            get { return null; }
#pragma warning restore IDE0025 // Use expression body for properties
        }

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
    }

    [Test]
    public void CorrelationIdInBaseClassSqlSaga()
    {
        var sagaType = module.GetTypeDefinition<SqlSagaWithDataBaseClass>();
        SagaDefinitionReader.TryGetSagaDefinition(sagaType, out var definition);
        Assert.That(definition, Is.Not.Null);
        Approver.Verify(definition);
    }

    public class SqlSagaWithDataBaseClass : SqlSaga<SqlSagaWithDataBaseClass.SagaData>
    {
        public abstract class SagaDataBaseClass : ContainSagaData
        {
            public string MyId { get; set; }
        }
        public class SagaData : SagaDataBaseClass
        {
        }

        protected override string TableSuffix => "TheTableSuffix";
        protected override string CorrelationPropertyName => nameof(SagaData.MyId);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
    }

    [Test]
    public void CorrelationIdInTwoLevelsDeepBaseClassSqlSaga()
    {
        var sagaType = module.GetTypeDefinition<SqlSagaWithTwoLevelsDeepDataBaseClass>();
        SagaDefinitionReader.TryGetSagaDefinition(sagaType, out var definition);
        Assert.That(definition, Is.Not.Null);
        Approver.Verify(definition);
    }

    public class SqlSagaWithTwoLevelsDeepDataBaseClass : SqlSaga<SqlSagaWithTwoLevelsDeepDataBaseClass.SagaData>
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

        protected override string TableSuffix => "TheTableSuffix";
        protected override string CorrelationPropertyName => nameof(SagaData.MyId);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
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