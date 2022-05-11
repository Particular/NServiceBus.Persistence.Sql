using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Settings;

public class ConfigureEndpointSqlPersistence : IConfigureEndpointTestExecution
{
    SetupAndTeardownDatabase setupFeature;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        if (configuration.IsSendOnly())
        {
            return Task.CompletedTask;
        }
        var tablePrefix = TestTableNameCleaner.Clean(endpointName, 30);

        configuration.RegisterStartupTask(sp =>
        {
            setupFeature = new SetupAndTeardownDatabase(
                sp.GetRequiredService<IReadOnlySettings>(),
                tablePrefix,
                MySqlConnectionBuilder.Build,
                BuildSqlDialect.MySql,
                e => e.Message.Contains("sqlpersistence_raiseerror already exists"));

            return setupFeature;
        });

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(MySqlConnectionBuilder.Build);
        persistence.SqlDialect<SqlDialect.MySql>();
        persistence.TablePrefix($"{tablePrefix}_");
        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.DisableCache();
        persistence.DisableInstaller();
        return Task.CompletedTask;
    }

    public Task Cleanup() => setupFeature != null ? setupFeature.ManualStop(CancellationToken.None) : Task.CompletedTask;
}