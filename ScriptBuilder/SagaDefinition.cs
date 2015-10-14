namespace NServiceBus.SqlPersistence
{
    public class SagaDefinition
    {
        public string Name { get; set; }
        public string CorrelationMember { get; set; }
    }
}