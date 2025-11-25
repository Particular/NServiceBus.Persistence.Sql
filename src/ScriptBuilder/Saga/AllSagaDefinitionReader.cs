#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class AllSagaDefinitionReader(ModuleDefinition module)
{
    public IList<SagaDefinition> GetSagas(Action<string, string>? logger = null)
    {
        var sagas = new List<SagaDefinition>();
        var errors = new List<Exception>();

        var attributes = module.Assembly.CustomAttributes
            .Where(att => att is not null && att.AttributeType.FullName == "NServiceBusGeneratedSqlSagaMetadataAttribute")
            .Select(att => att!)
            .ToImmutableArray();

        foreach (var att in attributes)
        {
            try
            {
                var sagaType = att.GetStringProperty("SagaType");
                var corrName = att.GetStringProperty("CorrelationPropertyName");
                var corrType = att.GetStringProperty("CorrelationPropertyType");
                var transName = att.GetStringProperty("TransitionalCorrelationPropertyName");
                var transType = att.GetStringProperty("TransitionalCorrelationPropertyType");
                var tableSuffix = att.GetStringProperty("TableSuffix");

                var correlation = GetCorrelation(corrName, corrType);
                var transitionalCorrelation = GetCorrelation(transName, transType);

                var definition = new SagaDefinition(tableSuffix, sagaType, correlation, transitionalCorrelation);
                sagas.Add(definition);
            }
            catch (Exception exception)
            {
                logger?.Invoke(exception.Message, att.ToString() ?? "<Missing attribute definition>");
                errors.Add(new Exception($"Error translating generated attribute '{att}' into saga metadata. Error: {exception.Message}", exception));
            }
        }

        if (errors.Count > 0 && logger == null)
        {
            throw new AggregateException(errors);
        }

        return sagas;
    }

    static CorrelationProperty? GetCorrelation(string? name, string? type)
    {
        if (name is null || type is null)
        {
            return null;
        }

        var propType = type switch
        {
            "string" => CorrelationPropertyType.String,
            _ => throw new Exception("Unknown correlation property type")
        };

        return new CorrelationProperty(name, propType);
    }

}