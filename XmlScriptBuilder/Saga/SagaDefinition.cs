
namespace NServiceBus.Persistence.Sql.Xml
{
    public class SagaDefinition
    {
        public string Name;
        public CorrelationMember CorrelationMember;
        public CorrelationMember TransitionalCorrelationMember;
    }
}