using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public class ConfigureEndpointHelper
{
    Func<DbConnection> connectionBuilder;
    BuildSqlVariant sqlVariant;
    Func<Exception, bool> exceptionFilter;
    string tablePrefix;
    List<SagaDefinition> sagaDefinitions;

    public ConfigureEndpointHelper(EndpointConfiguration configuration, string tablePrefix, Func<DbConnection> connectionBuilder, BuildSqlVariant sqlVariant, Func<Exception, bool> exceptionFilter = null)
    {
        this.tablePrefix = tablePrefix;
        this.connectionBuilder = connectionBuilder;
        this.sqlVariant = sqlVariant;
        this.exceptionFilter = exceptionFilter;
        sagaDefinitions = RuntimeSagaDefinitionReader.GetSagaDefinitions(configuration, sqlVariant).ToList();
        using (var connection = connectionBuilder())
        {
            connection.Open();
            foreach (var definition in sagaDefinitions)
            {
                connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), tablePrefix, exceptionFilter);
                try
                {
                    connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVariant), tablePrefix);
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("Can't DROP"))
                    {
                        throw; //ignore cleanup exceptions caused by async database operations
                    }
                }
            }
            connection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlVariant), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(TimeoutScriptBuilder.BuildCreateScript(sqlVariant), tablePrefix);
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlVariant), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildCreateScript(sqlVariant), tablePrefix);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlVariant), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(sqlVariant), tablePrefix);
        }
    }

    public Task Cleanup()
    {
        using (var connection = connectionBuilder())
        {
            connection.Open();
            foreach (var definition in sagaDefinitions)
            {
                connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), tablePrefix, exceptionFilter);
            }
            connection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlVariant), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlVariant), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlVariant), tablePrefix, exceptionFilter);
        }
        return Task.FromResult(0);
    }
}