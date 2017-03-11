using System;

namespace NServiceBus.Persistence.Sql
{
    [Obsolete("Replaced by " + nameof(CorrelatedSagaAttribute),true)]
    public class SqlSagaAttribute : Attribute
    {
        public string CorrelationProperty { get; }
        public string TransitionalCorrelationProperty { get; }
        public string TableSuffix { get; }

        public SqlSagaAttribute(string correlationProperty = null, string transitionalCorrelationProperty = null, string tableSuffix = null)
        {
            CorrelationProperty = correlationProperty;
            TransitionalCorrelationProperty = transitionalCorrelationProperty;
            TableSuffix = tableSuffix;
        }
    }
}