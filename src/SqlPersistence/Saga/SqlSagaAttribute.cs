using System;

namespace NServiceBus.Persistence.Sql
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SqlSagaAttribute : Attribute
    {
        public string CorrelationProperty;
        public string TransitionalCorrelationProperty;
        public string TableSuffix;
    }
}