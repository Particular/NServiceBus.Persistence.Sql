namespace NServiceBus.Persistence.Sql.ScriptBuilder
{
    public class CorrelationProperty
    {
        public CorrelationProperty(string name, CorrelationPropertyType type)
        {
            Guard.AgainstNullAndEmpty(nameof(name), name);
            Name = name;
            Type = type;
        }

        public CorrelationPropertyType Type { get; }

        public string Name { get; }
    }
}