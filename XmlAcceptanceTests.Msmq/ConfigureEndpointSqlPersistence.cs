using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence.SqlServerXml;

public class ConfigureEndpointSqlPersistence : IConfigureEndpointTestExecution
{
    public static string ConnectionString
    {
        get
        {
            var envVar = Environment.GetEnvironmentVariable("Sql_ACC_TEST_CONNSTR");
            if (!string.IsNullOrEmpty(envVar))
            {
                return envVar;
            }

            return defaultConnStr;
        }
    }

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
    {
        configuration.UsePersistence<SqlXmlPersistence>()
            .ConnectionString(ConnectionString);

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        return Task.FromResult(0);
    }

    const string defaultConnStr = @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;";
}