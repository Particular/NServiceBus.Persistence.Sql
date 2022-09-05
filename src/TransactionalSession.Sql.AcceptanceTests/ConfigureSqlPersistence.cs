namespace NServiceBus.AcceptanceTests;

using System.Threading.Tasks;
using AcceptanceTesting.Support;

public class ConfigureSqlPersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(MsSqlMicrosoftDataClientConnectionBuilder.Build);
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.DisableCache();
        persistence.DisableInstaller();

        //persistence.EnableTransactionalSession();

        return Task.CompletedTask;
    }

    Task IConfigureEndpointTestExecution.Cleanup() => Task.CompletedTask;
}