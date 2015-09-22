using System;
using NServiceBus.Saga;

class SagaPersister : ISagaPersister
{
    public void Save(IContainSagaData saga)
    {
        throw new NotImplementedException();
    }

    public void Update(IContainSagaData saga)
    {
        throw new NotImplementedException();
    }

    public TSagaData Get<TSagaData>(Guid sagaId) where TSagaData : IContainSagaData
    {
        throw new NotImplementedException();
    }

    public TSagaData Get<TSagaData>(string propertyName, object propertyValue) where TSagaData : IContainSagaData
    {
        throw new NotImplementedException();
    }

    public void Complete(IContainSagaData saga)
    {
        throw new NotImplementedException();
    }
}
