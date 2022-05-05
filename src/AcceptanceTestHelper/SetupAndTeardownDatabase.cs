using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Settings;

public class SetupAndTeardownDatabase : FeatureStartupTask
{
    Func<DbConnection> connectionBuilder;
    BuildSqlDialect sqlDialect;
    Func<Exception, bool> exceptionFilter;
    IReadOnlySettings settings;
    string tablePrefix;
    List<SagaDefinition> sagaDefinitions;

    public SetupAndTeardownDatabase(IReadOnlySettings settings, string tablePrefix, Func<DbConnection> connectionBuilder, BuildSqlDialect sqlDialect, Func<Exception, bool> exceptionFilter = null)
    {
        this.settings = settings;
        this.tablePrefix = tablePrefix;
        this.connectionBuilder = connectionBuilder;
        this.sqlDialect = sqlDialect;
        this.exceptionFilter = exceptionFilter;
    }

    protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
    {
        sagaDefinitions = RuntimeSagaDefinitionReader.GetSagaDefinitions(settings, sqlDialect).ToList();
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            foreach (var definition in sagaDefinitions)
            {
                connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlDialect), tablePrefix, exceptionFilter);
                try
                {
                    connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlDialect), tablePrefix);
                }
                catch (Exception exception) when (exception.Message.Contains("Can't DROP"))
                {
                    //ignore cleanup exceptions caused by async database operations
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

    protected override async Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
    {
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            foreach (var definition in sagaDefinitions)
            {
                connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlDialect), tablePrefix, exceptionFilter);
            }

            connection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
        }
    }
}