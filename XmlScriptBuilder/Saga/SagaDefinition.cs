namespace NServiceBus.Persistence.SqlServerXml
{
    public class SagaDefinition
    {
        public string Name;
        public CorrelationMember CorrelationMember;
        public CorrelationMember TransitionalCorrelationMember;
    }
}