namespace NServiceBus.Persistence.Sql
{
    using System;

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CorrelatedSagaAttribute : Attribute
    {
        public string CorrelationProperty { get; }
        public string TransitionalCorrelationProperty { get; set; }
        public string TableSuffix { get; set; }

        public CorrelatedSagaAttribute(string correlationProperty)
        {
            CorrelationProperty = correlationProperty;
        }
    }
}