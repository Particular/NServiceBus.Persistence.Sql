namespace NServiceBus.Persistence.Sql
{
    using System;

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AlwaysStartNewSagaAttribute : Attribute
    {
        public string TableSuffix { get; set; }
    }
}