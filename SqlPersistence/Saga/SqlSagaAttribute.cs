using System;

namespace NServiceBus.Persistence.Sql
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SqlSagaAttribute : Attribute
    {
        public string CorrelationProperty;
        public string TransitionalCorrelationProperty;
        public string TableSuffix;

        public SqlSagaAttribute(string correlationProperty, string transitionalCorrelationProperty = null, string tableSuffix = null)
        {
            CorrelationProperty = correlationProperty;
            TransitionalCorrelationProperty = transitionalCorrelationProperty;
            TableSuffix = tableSuffix;
            Guard.AgainstNullAndEmpty(nameof(correlationProperty), correlationProperty);
            Guard.AgainstEmpty(nameof(transitionalCorrelationProperty), transitionalCorrelationProperty);
            Guard.AgainstEmpty(nameof(tableSuffix), tableSuffix);
        }
    }
}