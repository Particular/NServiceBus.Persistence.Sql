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
public partial class CoreSagaMetadataTests
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

    [Test]
    public void SingleMappingValueType()
    {
        var dataType = module.GetTypeDefinition<SingleMappingValueTypeSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [Test]
    public void DualMapping()
    {
        var dataType = module.GetTypeDefinition<DualMappingSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [Test]
    public void DontMapWithIntermediateBase()
    {
        var dataType = module.GetTypeDefinition<HasBaseSagaClass>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
        Assert.IsNull(definition);
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

    [Test]
    public void DontAllowPassingMapper()
    {
        var dataType = module.GetTypeDefinition<PassingMapperSaga>();
        Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinitionReader.TryGetSagaDefinition(dataType, out _);
        });

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

    [Test]
    public void DontMapSwitchingLogic()
    {
        var dataType = module.GetTypeDefinition<SwitchingLogicSaga>();
        Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinitionReader.TryGetSagaDefinition(dataType, out _);
        });

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

    [Test]
    public void DontAllowLooping()
    {
        var dataType = module.GetTypeDefinition<ForLoopSaga>();
        Assert.Throws<ErrorsException>(() =>
        {
            SagaDefinitionReader.TryGetSagaDefinition(dataType, out _);
        });

    }

    [Test]
    public void AllowConcatenatingMsgProperties()
    {
        var dataType = module.GetTypeDefinition<ConcatMsgPropertiesSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [Test]
    public void AllowConcatenatingMsgPropertiesWithFormat()
    {
        var dataType = module.GetTypeDefinition<ConcatMsgPropertiesWithFormatSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [Test]
    public void AllowConcatenatingMsgPropertiesWithInterpolation()
    {
        var dataType = module.GetTypeDefinition<ConcatMsgPropertiesWithInterpolationSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    [Test]
    public void SupplyAdditionalMetadataViaAttribute()
    {
        var dataType = module.GetTypeDefinition<MetadataInAttributeSaga>();
        SagaDefinitionReader.TryGetSagaDefinition(dataType, out var definition);
        ObjectApprover.VerifyWithJson(definition);
    }

    




}