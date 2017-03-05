using System;

namespace NServiceBus.Persistence.Sql
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
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