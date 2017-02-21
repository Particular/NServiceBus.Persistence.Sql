using System;
using System.Data.SqlClient;
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
        var connectionString = @"Server=localhost\SqlExpress;Database=nservicebus;Trusted_Connection=True;";
        endpointHelper = new ConfigureEndpointHelper(configuration, endpointName, () => new SqlConnection(connectionString), BuildSqlVariant.MsSqlServer, FilterTableExists);
        var persistence = configuration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(() => new SqlConnection(connectionString));
        persistence.DisableInstaller();
        return Task.FromResult(0);
    }

    bool FilterTableExists(Exception exception)
    {
        return exception.Message.Contains("Cannot drop the table");
    }

    public Task Cleanup()
    {
        endpointHelper.Cleanup();
        return Task.FromResult(0);
    }
}