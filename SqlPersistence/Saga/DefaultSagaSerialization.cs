namespace NServiceBus.Persistence.Sql
{
    public class DefaultSagaSerialization<TReader>
    {
        public readonly SagaSerialize Serialize;
        public readonly SagaDeserialize<TReader> Deserialize;

        public DefaultSagaSerialization(SagaSerialize serialize, SagaDeserialize<TReader> deserialize)
        {
            Serialize = serialize;
            Deserialize = deserialize;
        }
    }
}