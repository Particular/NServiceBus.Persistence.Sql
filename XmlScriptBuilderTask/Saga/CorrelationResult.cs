using NServiceBus.Persistence.SqlServerXml;

class CorrelationResult
{
    public CorrelationMember CorrelationMember;
    public CorrelationMember TransitionalCorrelationMember;
}