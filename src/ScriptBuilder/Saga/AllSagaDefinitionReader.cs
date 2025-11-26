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
            try
            {
                var sagaType = GetValue(att, "SagaType");
                var corrName = GetValue(att, "CorrelationPropertyName");
                var corrType = GetValue(att, "CorrelationPropertyType");
                var transName = GetValue(att, "TransitionalCorrelationPropertyName");
                var transType = GetValue(att, "TransitionalCorrelationPropertyType");
                var tableSuffix = GetValue(att, "TableSuffix");

                var correlation = GetCorrelation(corrName, corrType);
                var transitionalCorrelation = GetCorrelation(transName, transType);

                var definition = new SagaDefinition(tableSuffix, sagaType, correlation, transitionalCorrelation);
                sagas.Add(definition);
            }
            catch (Exception exception)
            {
                logger?.Invoke(exception.Message, att.ToString());
                errors.Add(new Exception($"Error translating generated attribute '{att}' into saga metadata. Error: {exception.Message}", exception));
            }
        }

        if (errors.Count > 0 && logger == null)
        {
            throw new AggregateException(errors);
        }

        return sagas;
    }

    static string? GetValue(CustomAttributeData attribute, string attributeName)
        => attribute.NamedArguments?.FirstOrDefault(a => a.MemberName == attributeName).TypedValue.Value as string;

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