using NServiceBus.Saga;

namespace NServiceBus.SqlPersistence
{
    public abstract class XmlSaga<TSagaData> : Saga<TSagaData>
        where TSagaData : 
        XmlSagaData, 
        IContainSagaData, 
        new()
    {

        protected sealed override void ConfigureHowToFindSaga(SagaPropertyMapper<TSagaData> mapper)
        {
            var messagePropertyMapper = new MessagePropertyMapper<TSagaData>(mapper, new TSagaData());
            ConfigureHowToFindSaga(messagePropertyMapper);
        }

        protected virtual void ConfigureHowToFindSaga(MessagePropertyMapper<TSagaData> mapper)
        {
        }

    }
}