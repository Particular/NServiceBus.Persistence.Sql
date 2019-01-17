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
        Assert.IsNotNull(exception.Message);
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

        Assert.IsNotNull(definition);
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
        Assert.IsNotNull(exception.Message);
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
        Assert.IsNotNull(definition);
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
        Assert.IsNotNull(definition);
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
        Assert.IsNotNull(definition);
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
        Assert.IsNotNull(definition);
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
            get { return null; }
        }

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }
    }

    public void Dispose()
    {
        module?.Dispose();
    }
}