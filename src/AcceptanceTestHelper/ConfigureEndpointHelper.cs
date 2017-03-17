using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public class ConfigureEndpointHelper
{
    Func<DbConnection> connectionBuilder;
    BuildSqlVariant sqlVariant;
    Func<Exception, bool> exceptionFilter;
    string tablePrefix;
    List<SagaDefinition> sagaDefinitions;
    bool timeoutManagerEnabled;

    public ConfigureEndpointHelper(EndpointConfiguration configuration, string tablePrefix, Func<DbConnection> connectionBuilder, BuildSqlVariant sqlVariant, Func<Exception, bool> exceptionFilter = null)
    {
        this.tablePrefix = tablePrefix;
        this.connectionBuilder = connectionBuilder;
        this.sqlVariant = sqlVariant;
        this.exceptionFilter = exceptionFilter;
        sagaDefinitions = RuntimeSagaDefinitionReader.GetSagaDefinitions(configuration).ToList();
        using (var connection = connectionBuilder())
        {
            connection.Open();
            foreach (var definition in sagaDefinitions)
            {
                connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), tablePrefix, exceptionFilter);
                connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVariant), tablePrefix);
            }
            timeoutManagerEnabled = configuration.GetSettings().IsFeatureEnabled(typeof(TimeoutManager));
            if (timeoutManagerEnabled)
            {
                connection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlVariant), tablePrefix, exceptionFilter);
                connection.ExecuteCommand(TimeoutScriptBuilder.BuildCreateScript(sqlVariant), tablePrefix);
            }
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlVariant), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildCreateScript(sqlVariant), tablePrefix);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlVariant), tablePrefix, exceptionFilter);
            ExecuteOutboxCreateCommand(connection, OutboxScriptBuilder.BuildCreateScript(sqlVariant), tablePrefix, 10);
        }
    }

    static void ExecuteOutboxCreateCommand(DbConnection connection, string script, string tablePrefix, int inboxRowCount)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = script;
            command.AddParameter("tablePrefix", $"{tablePrefix}_");
            if (connection is SqlConnection)
            {
                command.AddParameter("schema", "dbo");
            }
            command.AddParameter("inboxRowCount", inboxRowCount);
            command.ExecuteNonQuery();
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
            if (timeoutManagerEnabled)
            {
                connection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlVariant), tablePrefix, exceptionFilter);
            }
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlVariant), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlVariant), tablePrefix, exceptionFilter);
        }
        return Task.FromResult(0);
    }
}