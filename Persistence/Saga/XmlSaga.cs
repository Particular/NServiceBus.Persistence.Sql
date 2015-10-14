using NServiceBus.Saga;

namespace NServiceBus.SqlPersistence
{
    public abstract class XmlSaga<TSagaData> : Saga<TSagaData>
        where TSagaData : 
        XmlSagaData, 
        IContainSagaData, 
        new()
    {
        
    }
}