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
        var tablePrefix = TestTableNameCleaner.Clean(endpointName);
        configuration.RegisterStartupTask(sp =>
        {
            setupFeature = new SetupAndTeardownDatabase(
                TestContext.CurrentContext.Test.ID,
                sp.GetRequiredService<IReadOnlySettings>(),
                tablePrefix,
                MsSqlMicrosoftDataClientConnectionBuilder.Build,
                BuildSqlDialect.MsSqlServer);

            return setupFeature;
        });

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(MsSqlMicrosoftDataClientConnectionBuilder.Build);
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.DisableCache();
        persistence.DisableInstaller();
        return Task.CompletedTask;
    }

    public Task Cleanup() => setupFeature != null ? setupFeature.ManualStop(CancellationToken.None) : Task.CompletedTask;
}