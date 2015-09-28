namespace NServiceBus.SqlPersistence.Saga
{
    public class DefualtSerialization
    {
        public readonly Serialize Serialize;
        public readonly Deserialize Deserialize;

        public DefualtSerialization(Serialize serialize, Deserialize deserialize)
        {
            Serialize = serialize;
            Deserialize = deserialize;
        }
    }
}