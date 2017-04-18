using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public class ConfigureEndpointSqlPersistence : IConfigureEndpointTestExecution
{
    ConfigureEndpointHelper endpointHelper;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        if (configuration.IsSendOnly())
        {
            return Task.FromResult(0);
        }
        // So that all sagas from Core acceptance tests fit within 64-character MySQL identifier limit
        var tablePrefix = "AT";
        endpointHelper = new ConfigureEndpointHelper(configuration, tablePrefix, MySqlConnectionBuilder.Build, BuildSqlVariant.MySql);
        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(MySqlConnectionBuilder.Build);
        persistence.SqlVariant(SqlVariant.MySql);
        persistence.TablePrefix($"{tablePrefix}_");
        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.DisableCache();
        persistence.DisableInstaller();
        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        return endpointHelper?.Cleanup();
    }
}