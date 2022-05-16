using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Settings;
using NUnit.Framework;

public class ConfigureEndpointSqlPersistence : IConfigureEndpointTestExecution
{
    SetupAndTeardownDatabase setupFeature;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        if (configuration.IsSendOnly())
        {
            return Task.CompletedTask;
        }

        var tablePrefix = TestTableNameCleaner.Clean(endpointName, 24);
        Console.WriteLine($"Using EndpointName='{endpointName}', TablePrefix='{tablePrefix}'");
        configuration.RegisterStartupTask(sp =>
        {
            setupFeature = new SetupAndTeardownDatabase(
                TestContext.CurrentContext.Test.ID,
                sp.GetRequiredService<IReadOnlySettings>(),
                tablePrefix,
                OracleConnectionBuilder.Build,
                BuildSqlDialect.Oracle);

            return setupFeature;
        });

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.Oracle>();
        persistence.ConnectionBuilder(OracleConnectionBuilder.Build);
        persistence.TablePrefix($"{tablePrefix}_");
        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.DisableCache();
        persistence.DisableInstaller();

        //Force Saga table names to 27 characters to fit in Oracle
        var sagaSettings = persistence.SagaSettings();
        sagaSettings.NameFilter(sagaName => sagaName.Substring(0, Math.Min(27, sagaName.Length)));

        return Task.CompletedTask;
    }

    public Task Cleanup() => setupFeature != null ? setupFeature.ManualStop(CancellationToken.None) : Task.CompletedTask;
}