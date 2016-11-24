
namespace NServiceBus.Persistence.Sql
{
    public class SagaDefinition
    {
        public string Name;
        public CorrelationMember CorrelationMember;
        public CorrelationMember TransitionalCorrelationMember;
    }
}