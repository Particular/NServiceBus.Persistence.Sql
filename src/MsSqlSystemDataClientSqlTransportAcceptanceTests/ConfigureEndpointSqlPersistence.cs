using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Settings;

public class ConfigureEndpointSqlPersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        if (configuration.IsSendOnly())
        {
            return Task.CompletedTask;
        }
        var tablePrefix = TableNameCleaner.Clean(endpointName);
        configuration.RegisterStartupTask(sp => new SetupAndTeardownDatabase(
            sp.GetRequiredService<IReadOnlySettings>(),
            tablePrefix,
            MsSqlSystemDataClientConnectionBuilder.Build,
            BuildSqlDialect.MsSqlServer));

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(MsSqlSystemDataClientConnectionBuilder.Build);
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.DisableCache();
        persistence.DisableInstaller();
        return Task.CompletedTask;
    }

    public Task Cleanup() =>
        //Cleanup is made in the SetupAndTeardownDatabase feature OnStop method 
        Task.CompletedTask;
}