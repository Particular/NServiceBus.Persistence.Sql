using System;
using System.Collections.Generic;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class AllSagaDefinitionReader
{
    readonly ModuleDefinition module;

    public AllSagaDefinitionReader(ModuleDefinition module)
    {
        this.module = module;
    }

    public IList<SagaDefinition> GetSagas(Action<string, string> logger = null)
    {
        var sagas = new List<SagaDefinition>();
        var errors = new List<Exception>();

        foreach (var type in module.AllClasses())
        {
            try
            {
                if (SagaDefinitionReader.TryGetSagaDefinition(type, out var definition))
                {
                    sagas.Add(definition);
                }
            }
            catch (ErrorsException exception)
            {
                logger?.Invoke(exception.Message, type.FullName);
                errors.Add(new Exception($"Error in '{type.FullName}' (Filename='{type.GetFileName()}'). Error: {exception.Message}", exception));
            }
        }

        if (errors.Count > 0 && logger == null)
        {
            throw new AggregateException(errors);
        }

        return sagas;
    }
}