
namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    public class SagaDefinition
    {
        public SagaDefinition(string tableSuffix, string name, CorrelationProperty correlationProperty, CorrelationProperty transitionalCorrelationProperty = null)
        {
            Guard.AgainstNullAndEmpty(nameof(tableSuffix), tableSuffix);
            Guard.AgainstNullAndEmpty(nameof(name), name);
            Guard.AgainstNull(nameof(correlationProperty), correlationProperty);
            TableSuffix = tableSuffix;
            Name = name;
            CorrelationProperty = correlationProperty;
            TransitionalCorrelationProperty = transitionalCorrelationProperty;
        }

        public SagaDefinition(string tableSuffix, string name)
        {
            Guard.AgainstNullAndEmpty(nameof(tableSuffix), tableSuffix);
            Guard.AgainstNullAndEmpty(nameof(name), name);
            TableSuffix = tableSuffix;
            Name = name;
            IsAlwaysStartNew = true;
        }

        public string TableSuffix { get; }
        public CorrelationProperty CorrelationProperty { get; }
        public CorrelationProperty TransitionalCorrelationProperty { get; }
        public string Name { get; }
        public bool IsAlwaysStartNew { get; }
    }
}