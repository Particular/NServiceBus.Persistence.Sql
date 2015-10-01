using System;
using System.IO;
using Mono.Cecil;
using NServiceBus.Saga;
using NUnit.Framework;
using ReferenceLibrary;


[TestFixture]
public class SagaMetaDataReaderTest
{
    ModuleDefinition module;

    public SagaMetaDataReaderTest()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "MsBuildTests.dll");
        module = ModuleDefinition.ReadModule(path);
    }

    [Test]
    public void FindSagaDataType()
    {
        TypeReference sagaDataType;
        var sagaType = module.GetTypeDefinition<StandardSaga>();
        var sagaMetaDataReader = new SagaMetaDataReader(module);
        Assert.IsTrue(sagaMetaDataReader.FindSagaDataType(sagaType, out sagaDataType));
        Assert.AreEqual(typeof(StandardSaga.SagaDaga).FullName, sagaDataType.FullName.Replace("/", "+"));
    }

    public class StandardSaga : Saga<StandardSaga.SagaDaga>
    {
        public class SagaDaga : ContainSagaData
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDaga> mapper)
        {
        }
    }

    [Test]
    public void FindSagaDataType_from_another_assembly()
    {
        TypeReference sagaDataType;
        var sagaType = module.GetTypeDefinition<ChildSaga>();
        var sagaMetaDataReader = new SagaMetaDataReader(module);
        Assert.IsTrue(sagaMetaDataReader.FindSagaDataType(sagaType, out sagaDataType));
        Assert.AreEqual(typeof(BaseSagaData).FullName, sagaDataType.FullName.Replace("/", "+"));
    }

    public class ChildSaga : BaseSaga
    {
    }

    [Test]
    public void FindSagaDataType_abstract()
    {
        TypeReference sagaDataType;
        var sagaType = module.GetTypeDefinition<AbstractSaga>();
        var sagaMetaDataReader = new SagaMetaDataReader(module);
        Assert.IsTrue(sagaMetaDataReader.FindSagaDataType(sagaType, out sagaDataType));
        Assert.AreEqual(typeof(AbstractSaga.SagaDaga).FullName, sagaDataType.FullName.Replace("/", "+"));
    }

    public abstract class AbstractSaga : Saga<AbstractSaga.SagaDaga>
    {
        public class SagaDaga : ContainSagaData
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDaga> mapper)
        {
        }
    }

    [Test]
    public void FindSagaDataType_Inherited()
    {
        TypeReference sagaDataType;
        var sagaMetaDataReader = new SagaMetaDataReader(module);
        var sagaType = module.GetTypeDefinition<InheritedSaga>();
        Assert.IsTrue(sagaMetaDataReader.FindSagaDataType(sagaType, out sagaDataType));
        Assert.AreEqual(typeof(StandardSaga.SagaDaga).FullName, sagaDataType.FullName.Replace("/","+"));
    }

    public class InheritedSaga : StandardSaga
    {
    }

    [Test]
    public void FindSagaDataType_not_a_generic_saga()
    {
        TypeReference sagaDataType;
        var sagaMetaDataReader = new SagaMetaDataReader(module);
        var sagaType = module.GetTypeDefinition<NotASaga>();
        Assert.IsFalse(sagaMetaDataReader.FindSagaDataType(sagaType, out sagaDataType));
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