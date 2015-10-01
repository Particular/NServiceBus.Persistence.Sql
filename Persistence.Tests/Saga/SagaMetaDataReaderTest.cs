using System;
using NServiceBus.Saga;
using NUnit.Framework;

[TestFixture]
public class SagaMetaDataReaderTest
{
    [Test]
    public void TryGetBaseSagaType()
    {
        Type sagaDataType;
        Assert.IsTrue(SagaMetaDataReader.TryGetSagaDataType(typeof(StandardSaga), out sagaDataType));
        Assert.AreEqual(typeof(StandardSaga.SagaDaga), sagaDataType);
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
    public void TryGetBaseSagaType_Inherited()
    {
        Type sagaDataType;
        Assert.IsTrue(SagaMetaDataReader.TryGetSagaDataType(typeof(InheritedSaga), out sagaDataType));
        Assert.AreEqual(typeof(StandardSaga.SagaDaga), sagaDataType);
    }

    public class InheritedSaga : StandardSaga
    {
    }

    [Test]
    public void TryGetBaseSagaType_not_a_generic_saga()
    {
        Type sagaDataType;
        Assert.IsFalse(SagaMetaDataReader.TryGetSagaDataType(typeof(NotASaga), out sagaDataType));
        Assert.IsNull(sagaDataType);
    }

    public class NotASaga :Saga
    {
        protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
        {
            throw new NotImplementedException();
        }
    }
}