namespace NServiceBus.Persistence.Sql
{
    public class DefaultSagaSerialization
    {
        public readonly SagaSerialize Serialize;
        public readonly SagaDeserialize Deserialize;

        public DefaultSagaSerialization(SagaSerialize serialize, SagaDeserialize deserialize)
        {
            Serialize = serialize;
            Deserialize = deserialize;
        }
    }
}