using System.Collections.Generic;
using Mono.Cecil;
using NServiceBus.Persistence.Sql.Xml;

class AllSagaDefinitionReader
{
    ModuleDefinition module;
    BuildLogger buildLogger;

    public AllSagaDefinitionReader(ModuleDefinition module, BuildLogger buildLogger)
    {
        this.module = module;
        this.buildLogger = buildLogger;
    }

    public IEnumerable<SagaDefinition> GetSagas()
    {
        var sagas = new List<SagaDefinition>();
        foreach (var type in module.AllClasses())
        {
            try
            {
                SagaDefinition definition;
                if (SagaDefinitionReader.TryGetSqlSagaDefinition(type, out definition))
                {
                    sagas.Add(definition);
                }
            }
            catch (ErrorsException exception)
            {
                buildLogger?.LogError($"Error in '{type.FullName}'. Error:{exception.Message}", type.GetFileName());
            }
        }
        return sagas;
    }



}