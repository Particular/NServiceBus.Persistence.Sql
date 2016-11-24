using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence.Sql;

public class ConfigureEndpointSqlXmlPersistence : EndpointConfigurer
{
    public override Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        var persistenceExtensions = configuration.UsePersistence<SqlXmlPersistence>();
        persistenceExtensions.ConnectionString(ConnectionString);
        persistenceExtensions.UseEndpointName(false);

        return Task.FromResult(0);
    }
}