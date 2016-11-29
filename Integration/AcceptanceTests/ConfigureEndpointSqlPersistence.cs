using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence.Sql;

public class ConfigureEndpointSqlPersistence : EndpointConfigurer
{
    public override Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionString(ConnectionString);
        persistence.UseEndpointName(false);

        return Task.FromResult(0);
    }
}