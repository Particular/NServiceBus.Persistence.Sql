using System;

namespace NServiceBus.Persistence.Sql
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SqlSagaAttribute : Attribute
    {
        public string CorrelationProperty { get; }
        public string TransitionalCorrelationProperty;
        public string TableSuffix;

        public SqlSagaAttribute(string correlationProperty)
        {
            CorrelationProperty = correlationProperty;
        }
    }
}