using System;
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
        var tablePrefix = TableNameCleaner.Clean(endpointName).Substring(0, Math.Min(endpointName.Length, 30));
        configuration.RegisterStartupTask(sp => new SetupAndTeardownDatabase(
            sp.GetRequiredService<IReadOnlySettings>(),
            tablePrefix,
            MySqlConnectionBuilder.Build,
            BuildSqlDialect.MySql));

        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(MySqlConnectionBuilder.Build);
        persistence.SqlDialect<SqlDialect.MySql>();
        persistence.TablePrefix($"{tablePrefix}_");
        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.DisableCache();
        persistence.DisableInstaller();
        return Task.CompletedTask;
    }

    public Task Cleanup() =>
        //Cleanup is made in the SetupAndTeardownDatabase feature OnStop method 
        Task.CompletedTask;
}