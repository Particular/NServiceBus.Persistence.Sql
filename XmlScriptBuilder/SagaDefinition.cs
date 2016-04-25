namespace NServiceBus.Persistence.SqlServerXml
{
    public class SagaDefinition
    {
        public string Name { get; set; }
        public string CorrelationMember { get; set; }
    }
}