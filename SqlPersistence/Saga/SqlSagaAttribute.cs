using System;

namespace NServiceBus.Persistence.Sql
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SqlSagaAttribute : Attribute
    {
        public string CorrelationId;
        public string TransitionalCorrelationId;
        public string TableSuffix;

        public SqlSagaAttribute(string correlationId, string transitionalCorrelationId = null, string tableSuffix = null)
        {
            CorrelationId = correlationId;
            TransitionalCorrelationId = transitionalCorrelationId;
            TableSuffix = tableSuffix;
            Guard.AgainstNullAndEmpty(nameof(correlationId), correlationId);
            Guard.AgainstEmpty(nameof(transitionalCorrelationId), transitionalCorrelationId);
            Guard.AgainstEmpty(nameof(tableSuffix), tableSuffix);
        }
    }
}