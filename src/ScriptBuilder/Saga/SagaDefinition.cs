#nullable enable

namespace NServiceBus.Persistence.Sql.ScriptBuilder;

using System;

public class SagaDefinition
{
    public SagaDefinition(string tableSuffix, string name, CorrelationProperty? correlationProperty = null, CorrelationProperty? transitionalCorrelationProperty = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(tableSuffix, nameof(tableSuffix));
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        TableSuffix = tableSuffix;
        Name = name;
        CorrelationProperty = correlationProperty;
        TransitionalCorrelationProperty = transitionalCorrelationProperty;
    }

    public string TableSuffix { get; }
    public CorrelationProperty? CorrelationProperty { get; }
    public CorrelationProperty? TransitionalCorrelationProperty { get; }
    public string Name { get; }
}