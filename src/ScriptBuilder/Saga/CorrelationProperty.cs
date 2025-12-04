namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    using System;

    public class CorrelationProperty
    {
        public CorrelationProperty(string name, CorrelationPropertyType type)
        {
            ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
            Name = name;
            Type = type;
        }

        public CorrelationPropertyType Type { get; }

        public string Name { get; }
    }
}