namespace NServiceBus.AcceptanceTests;

using System.Threading.Tasks;
using AcceptanceTesting.Support;
using TransactionalSession;

public class ConfigureEndpointSqlPersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(MsSqlMicrosoftDataClientConnectionBuilder.BuildWithoutCertificateCheck);
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.DisableCache();
        persistence.DisableInstaller();
        persistence.EnableTransactionalSession();

        return Task.CompletedTask;
    }

    Task IConfigureEndpointTestExecution.Cleanup() => Task.CompletedTask;
}