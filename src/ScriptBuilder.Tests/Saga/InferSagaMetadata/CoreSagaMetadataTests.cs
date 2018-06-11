using System;
using System.IO;
using Mono.Cecil;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;
#if NET452
using ObjectApproval;
#endif

[TestFixture]
public class CoreSagaMetadataTests
{
    ModuleDefinition module;

#if NETCOREAPP2_0
    static class ObjectApprover
    {
        public static void VerifyWithJson(object definition)
        {
            // Stub for approval tests that don't run for netcoreapp2.0 build target
        }
    }
#endif

    public CoreSagaMetadataTests()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilder.Tests.dll");
        var readerParameters = new ReaderParameters(ReadingMode.Deferred);
        module = ModuleDefinition.ReadModule(path, readerParameters);
    }

    [Test]
    public void SingleMapping()
    {
        var dataType = module.GetTypeDefinition<SingleMappingSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
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
    public void SingleMappingValueType()
    {
        var dataType = module.GetTypeDefinition<SingleMappingValueTypeSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    public class SingleMappingValueTypeSaga : Saga<SingleMappingValueTypeSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public int Correlation { get; set; }
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
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
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
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
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
        where TSaga : class, IContainSagaData, new()
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
            SagaDefinitionReader.TryGetSagaDefinition(dataType, out var _);
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
            SagaDefinitionReader.TryGetSagaDefinition(dataType, out _);
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
            SagaDefinitionReader.TryGetSagaDefinition(dataType, out _);
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
            SagaDefinitionReader.TryGetSagaDefinition(dataType, out _);
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
            SagaDefinitionReader.TryGetSagaDefinition(dataType, out _);
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
            SagaDefinitionReader.TryGetSagaDefinition(dataType, out _);
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

    [Test]
    public void AllowConcatenatingMsgProperties()
    {
        var dataType = module.GetTypeDefinition<ConcatMsgPropertiesSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    public class ConcatMsgPropertiesSaga : Saga<ConcatMsgPropertiesSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageC>(msg => msg.Part1 + msg.Part2).ToSaga(saga => saga.Correlation);
        }
    }

    [Test]
    public void AllowConcatenatingMsgPropertiesWithFormat()
    {
        var dataType = module.GetTypeDefinition<ConcatMsgPropertiesWithFormatSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    public class ConcatMsgPropertiesWithFormatSaga : Saga<ConcatMsgPropertiesWithFormatSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageC>(msg => string.Format("{0}{1}", msg.Part1, msg.Part2)).ToSaga(saga => saga.Correlation);
        }
    }

    [Test]
    public void AllowConcatenatingMsgPropertiesWithInterpolation()
    {
        var dataType = module.GetTypeDefinition<ConcatMsgPropertiesWithInterpolationSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    public class ConcatMsgPropertiesWithInterpolationSaga : Saga<ConcatMsgPropertiesWithInterpolationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageC>(msg => $"{msg.Part1}{msg.Part2}").ToSaga(saga => saga.Correlation);
        }
    }

    [Test]
    public void SupplyAdditionalMetadataViaAttribute()
    {
        var dataType = module.GetTypeDefinition<MetadataInAttributeSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [SqlSaga(transitionalCorrelationProperty: "TransitionalCorrId", tableSuffix: "DifferentTableSuffix")]
    public class MetadataInAttributeSaga : Saga<MetadataInAttributeSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string TransitionalCorrId { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
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

    public class MessageC : ICommand
    {
        public string Part1 { get; set; }
        public string Part2 { get; set; }
    }

}