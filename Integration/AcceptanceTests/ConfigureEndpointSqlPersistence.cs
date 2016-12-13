using System.Data.SqlClient;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence.Sql;

public class ConfigureEndpointSqlPersistence : EndpointConfigurer
{
    public override Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(async () =>
        {
            var sqlConnection = new SqlConnection(ConnectionString);
            await sqlConnection.OpenAsync();
            return sqlConnection;
        });
        persistence.TablePrefix("AcceptanceTests");

        return Task.FromResult(0);
    }
}