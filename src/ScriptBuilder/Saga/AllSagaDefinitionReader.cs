#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class AllSagaDefinitionReader(Assembly assembly)
{
    public IList<SagaDefinition> GetSagas(Action<string, string>? logger = null)
    {
        var sagas = new List<SagaDefinition>();
        var errors = new List<Exception>();

        var attributes = assembly.CustomAttributes
            .Where(att => att.AttributeType.FullName == "NServiceBusGeneratedSqlSagaMetadataAttribute")
            .ToImmutableArray();

        foreach (var att in attributes)
        {
            var properties = att.NamedArguments.ToDictionary(arg => arg.MemberName, arg => arg.TypedValue.Value as string);

            try
            {
                var sagaType = properties.GetValueOrDefault("SagaType");
                var corrName = properties.GetValueOrDefault("CorrelationPropertyName");
                var corrType = properties.GetValueOrDefault("CorrelationPropertyType");
                var transName = properties.GetValueOrDefault("TransitionalCorrelationPropertyName");
                var transType = properties.GetValueOrDefault("TransitionalCorrelationPropertyType");
                var tableSuffix = properties.GetValueOrDefault("TableSuffix");

                var correlation = GetCorrelation(corrName, corrType);
                var transitionalCorrelation = GetCorrelation(transName, transType);

                if (tableSuffix is not null && sagaType is not null)
                {
                    var definition = new SagaDefinition(tableSuffix, sagaType, correlation, transitionalCorrelation);
                    sagas.Add(definition);
                }
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
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        if (!Enum.TryParse<CorrelationPropertyType>(type, out var propType))
        {
            throw new Exception($"Invalid correlation property type '{type}' found in metadata attribute.");
        }

        return new CorrelationProperty(name, propType);
    }
}