using System;
using System.IO;
using Mono.Cecil;
using NServiceBus.Saga;
using NUnit.Framework;
using ReferenceLibrary;


public class SagaData : ContainSagaData
{
}
[TestFixture]
public class SagaMetaDataReaderTest
{
    ModuleDefinition module;

    public SagaMetaDataReaderTest()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptBuilderTask.Tests.dll");
        module = ModuleDefinition.ReadModule(path);
    }

    [Test]
    public void FindSagaDataType()
    {
        SagaDataMap map;
        var sagaType = module.GetTypeDefinition<StandardSaga>();
        var reader = new SagaMetaDataReader(module);
        Assert.IsTrue(reader.FindSagaDataType(sagaType, out map));
        Assert.AreEqual(typeof(SagaData).FullName, map.Data.FullName.Replace("/", "+"));
        Assert.AreEqual(typeof(StandardSaga).FullName, map.Saga.FullName.Replace("/", "+"));
    }

    public class StandardSaga : Saga<SagaData>
    {

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void FindSagaDataType_from_another_assembly()
    {
        SagaDataMap map;
        var sagaType = module.GetTypeDefinition<ChildFromAnotherAssemblySaga>();
        var reader = new SagaMetaDataReader(module);
        Assert.IsTrue(reader.FindSagaDataType(sagaType, out map));
        Assert.AreEqual(typeof(BaseSagaData).FullName, map.Data.FullName.Replace("/", "+"));
        Assert.AreEqual(typeof(ChildFromAnotherAssemblySaga).FullName, map.Saga.FullName.Replace("/", "+"));
    }

    public class ChildFromAnotherAssemblySaga : BaseInAnotherAssemblySaga
    {
    }
    [Test]
    public void FindSagaDataType_generic_from_another_assembly()
    {
        SagaDataMap map;
        var sagaType = module.GetTypeDefinition<ChildGenericFromAnotherAssemblySaga>();
        var reader = new SagaMetaDataReader(module);
        Assert.IsTrue(reader.FindSagaDataType(sagaType, out map));
        Assert.AreEqual(typeof(SagaData).FullName, map.Data.FullName.Replace("/", "+"));
        Assert.AreEqual(typeof(ChildGenericFromAnotherAssemblySaga).FullName, map.Saga.FullName.Replace("/", "+"));
    }

    public class ChildGenericFromAnotherAssemblySaga : GenericBaseInAnotherAssemblySaga<SagaData>
    {
    }

    [Test]
    public void FindSagaDataType_abstract()
    {
        SagaDataMap map;
        var sagaType = module.GetTypeDefinition<AbstractSaga>();
        var reader = new SagaMetaDataReader(module);
        Assert.IsTrue(reader.FindSagaDataType(sagaType, out map));
        Assert.AreEqual(typeof(SagaData).FullName, map.Data.FullName.Replace("/", "+"));
        Assert.AreEqual(typeof(AbstractSaga).FullName, map.Saga.FullName.Replace("/", "+"));
    }

    public abstract class AbstractSaga : Saga<SagaData>
    {

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void FindSagaDataType_Inherited()
    {
        SagaDataMap map;
        var reader = new SagaMetaDataReader(module);
        var sagaType = module.GetTypeDefinition<InheritedSaga>();
        Assert.IsTrue(reader.FindSagaDataType(sagaType, out map));
        Assert.AreEqual(typeof(SagaData).FullName, map.Data.FullName.Replace("/","+"));
        Assert.AreEqual(typeof(InheritedSaga).FullName, map.Saga.FullName.Replace("/","+"));
    }

    public class InheritedSaga : StandardSaga
    {
    }
    [Test]
    public void FindSagaDataType_Inherited_with_generic()
    {
        SagaDataMap map;
        var reader = new SagaMetaDataReader(module);
        var sagaType = module.GetTypeDefinition<InheritedFromSagaWithGeneric>();
        Assert.IsTrue(reader.FindSagaDataType(sagaType, out map));
        Assert.AreEqual(typeof(SagaData).FullName, map.Data.FullName.Replace("/","+"));
        Assert.AreEqual(typeof(InheritedFromSagaWithGeneric).FullName, map.Saga.FullName.Replace("/","+"));
    }

    public abstract class SagaWithGenric<T,K> : Saga<K> where K : IContainSagaData, new()
    {
    }

    public class InheritedFromSagaWithGeneric : SagaWithGenric<short, SagaData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }

    [Test]
    public void FindSagaDataType_not_a_generic_saga()
    {
        SagaDataMap sagaDataType;
        var reader = new SagaMetaDataReader(module);
        var sagaType = module.GetTypeDefinition<NotAGenericSaga>();
        Assert.IsFalse(reader.FindSagaDataType(sagaType, out sagaDataType));
        Assert.IsNull(sagaDataType);
    }

    public class NotAGenericSaga : Saga
    {
        protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
        {
            throw new NotImplementedException();
        }
    }
    [Test]
    public void FindSagaDataType_not_a_saga()
    {
        SagaDataMap sagaDataType;
        var reader = new SagaMetaDataReader(module);
        var sagaType = module.GetTypeDefinition<NotASaga>();
        Assert.IsFalse(reader.FindSagaDataType(sagaType, out sagaDataType));
        Assert.IsNull(sagaDataType);
    }

    public class NotASaga : Saga
    {
        protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
        {
            throw new NotImplementedException();
        }
    }
}