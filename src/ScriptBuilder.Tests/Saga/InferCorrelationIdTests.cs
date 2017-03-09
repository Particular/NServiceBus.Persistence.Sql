using System;
using System.IO;
using Mono.Cecil;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class InferCorrelationIdTests
{
    ModuleDefinition module;

    public InferCorrelationIdTests()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
        var readerParameters = new ReaderParameters(ReadingMode.Deferred);
        module = ModuleDefinition.ReadModule(path, readerParameters);
    }

    [Test]
    public void SingleMapping()
    {
        var dataType = module.GetTypeDefinition<SingleMappingSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    public class SingleMappingSaga : Saga<SingleMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
        }
    }

    [Test]
    public void DualMapping()
    {
        var dataType = module.GetTypeDefinition<DualMappingSaga>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    public class DualMappingSaga : Saga<DualMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            mapper.ConfigureMapping<MessageB>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
        }
    }

    [Test]
    public void DontMapWithIntermediateBase()
    {
        var dataType = module.GetTypeDefinition<HasBaseSagaClass>();
        SagaDefinition definition;
        SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        Assert.IsNull(definition);
    }

    public class HasBaseSagaClass : BaseSaga<HasBaseSagaClass.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            base.ConfigureHowToFindSaga(mapper);
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
        }
    }

    public class BaseSaga<TSaga> : Saga<TSaga>
        where TSaga : IContainSagaData, new()
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TSaga> mapper)
        {

        }
    }

    [Test]
    public void DontAllowMethodCallInMapping()
    {
        var dataType = module.GetTypeDefinition<MethodCallInMappingSaga>();
        Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinition definition;
            SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        });

    }

    public class MethodCallInMappingSaga : Saga<MethodCallInMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => MapInMethod(saga));
        }

        static object MapInMethod(SagaData data)
        {
            return data.Correlation;
        }
    }

    [Test]
    public void DontAllowPassingMapper()
    {
        var dataType = module.GetTypeDefinition<PassingMapperSaga>();
        Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinition definition;
            SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        });

    }

    public class PassingMapperSaga : Saga<PassingMapperSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            PassTheMapper(mapper);
        }

        static void PassTheMapper(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
        }
    }

    [Test]
    public void DontMapConflictingCorrelationIds()
    {
        var dataType = module.GetTypeDefinition<ConflictingCorrelationSaga>();
        Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinition definition;
            SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        });

    }

    public class ConflictingCorrelationSaga : Saga<ConflictingCorrelationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string OtherProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            mapper.ConfigureMapping<MessageB>(msg => msg.Correlation).ToSaga(saga => saga.OtherProperty);
        }
    }

    [Test]
    public void DontMapSwitchingLogic()
    {
        var dataType = module.GetTypeDefinition<SwitchingLogicSaga>();
        Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinition definition;
            SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        });

    }

    public class SwitchingLogicSaga : Saga<SwitchingLogicSaga.SagaData>
    {
        int number = 0;

        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string OtherProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            if (number > 0)
            {
                mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            }
            else
            {
                mapper.ConfigureMapping<MessageB>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            }
        }
    }

    [Test]
    public void DontMapDelegateCalls()
    {
        var dataType = module.GetTypeDefinition<DelegateCallingSaga>();
        Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinition definition;
            SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        });

    }

    public class DelegateCallingSaga : Saga<DelegateCallingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            Action action = () => mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            action();
        }
    }
    [Test]
    public void DontAllowLooping()
    {
        var dataType = module.GetTypeDefinition<LoopingSaga>();
        Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinition definition;
            SagaDefinitionReader.TryGetSqlSagaDefinition(dataType, out definition);
        });

    }

    public class LoopingSaga : Saga<LoopingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            for (var i = 0; i < 3; i++)
            {
                mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            }
        }
    }

    public class MessageA : ICommand
    {
        public string Correlation { get; set; }
    }

    public class MessageB : ICommand
    {
        public string Correlation { get; set; }
    }

}