using System;
using System.Collections.Concurrent;
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
    static ConcurrentDictionary<string, SemaphoreSlim> endpointSetupSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

    Func<DbConnection> connectionBuilder;
    BuildSqlDialect sqlDialect;
    Func<Exception, bool> exceptionFilter;
    IReadOnlySettings settings;
    string tablePrefix;
    string testId;
    List<SagaDefinition> sagaDefinitions;

    public SetupAndTeardownDatabase(string testId, IReadOnlySettings settings, string tablePrefix,
        Func<DbConnection> connectionBuilder, BuildSqlDialect sqlDialect, Func<Exception, bool> exceptionFilter = null)
    {
        this.testId = testId;
        this.settings = settings;
        this.tablePrefix = tablePrefix;
        this.connectionBuilder = connectionBuilder;
        this.sqlDialect = sqlDialect;
        this.exceptionFilter = exceptionFilter;
    }

    protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
    {
        var semaphore = endpointSetupSemaphores.GetOrAdd(testId, _ => new SemaphoreSlim(1));

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            sagaDefinitions = RuntimeSagaDefinitionReader.GetSagaDefinitions(settings, sqlDialect).ToList();
            using (var connection = connectionBuilder())
            {
                await connection.OpenAsync(cancellationToken);
                foreach (var definition in sagaDefinitions)
                {
                    connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlDialect), tablePrefix, exceptionFilter);
                    try
                    {
                        connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlDialect), tablePrefix, exceptionFilter);
                    }
                    catch (Exception exception) when (exception.Message.Contains("Can't DROP"))
                    {
                        //ignore cleanup exceptions caused by async database operations
                    }
                }

                connection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
                connection.ExecuteCommand(TimeoutScriptBuilder.BuildCreateScript(sqlDialect), tablePrefix, exceptionFilter);
                connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
                connection.ExecuteCommand(SubscriptionScriptBuilder.BuildCreateScript(sqlDialect), tablePrefix, exceptionFilter);
                connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
                connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(sqlDialect), tablePrefix, exceptionFilter);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task ManualStop(CancellationToken cancellationToken = default)
    {
        var semaphore = endpointSetupSemaphores.GetOrAdd(testId, _ => new SemaphoreSlim(1));

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            using (var connection = connectionBuilder())
            {
                await connection.OpenAsync(cancellationToken);

                if (sagaDefinitions != null)
                {
                    foreach (var definition in sagaDefinitions)
                    {
                        connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlDialect), tablePrefix, exceptionFilter);
                    }
                }

                connection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
                connection.ExecuteCommand(SubscriptionScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
                connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlDialect), tablePrefix, exceptionFilter);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }
}