namespace NServiceBus.Persistence.Sql
{
    public delegate IContainSagaData SagaDeserialize<TReader>(TReader reader);
}