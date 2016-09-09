using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public abstract class EndpointConfigurer : IConfigureEndpointTestExecution
{
    const string defaultConnStr = @"Server=localhost\SqlExpress;Database=nservicebus;Trusted_Connection=True;";

    public static string ConnectionString
    {
        get
        {
            var envVar = Environment.GetEnvironmentVariable("NH_ACC_TEST_CONNSTR");
            if (!string.IsNullOrEmpty(envVar))
            {
                return envVar;
            }

            return defaultConnStr;
        }
    }

    public abstract Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings);

    public Task Cleanup()
    {
        return Task.FromResult(0);
    }
}