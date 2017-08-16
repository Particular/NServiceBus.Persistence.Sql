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
    BuildSqlDialect sqlDialect;
    Func<Exception, bool> exceptionFilter;
    string tablePrefix;
    List<SagaDefinition> sagaDefinitions;

    public ConfigureEndpointHelper(EndpointConfiguration configuration, string tablePrefix, Func<DbConnection> connectionBuilder, BuildSqlDialect sqlDialect, Func<Exception, bool> exceptionFilter = null)
    {
        this.tablePrefix = tablePrefix;
        this.connectionBuilder = connectionBuilder;
        this.sqlDialect = sqlDialect;
        this.exceptionFilter = exceptionFilter;
        sagaDefinitions = RuntimeSagaDefinitionReader.GetSagaDefinitions(configuration, sqlDialect).ToList();
        using (var connection = connectionBuilder())
        {
            connection.Open();
            foreach (var definition in sagaDefinitions)
            {
                connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlDialect), tablePrefix, exceptionFilter);
                try
                {
                    connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlDialect), tablePrefix);
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("Can't DROP"))
                    {
                        throw; //ignore cleanup exceptions caused by async database operations
                    }
                }
            }
            connection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(TimeoutScriptBuilder.BuildCreateScript(sqlDialect), tablePrefix);
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildCreateScript(sqlDialect), tablePrefix);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(sqlDialect), tablePrefix);
        }
    }

    public Task Cleanup()
    {
        using (var connection = connectionBuilder())
        {
            connection.Open();
            foreach (var definition in sagaDefinitions)
            {
                connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlDialect), tablePrefix, exceptionFilter);
            }
            connection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
        }
        return Task.FromResult(0);
    }
}