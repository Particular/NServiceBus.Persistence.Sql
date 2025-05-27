namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.Collections.Concurrent;
using System.Reflection;
using AcceptanceTesting;

public class TransactionalSessionTestContext : ScenarioContext
{
    public IServiceProvider ServiceProvider
    {
        get
        {
            var property = typeof(ScenarioContext).GetProperty("CurrentEndpoint", BindingFlags.NonPublic | BindingFlags.Static);

            if (property!.GetValue(this) is not string endpointName)
            {
                throw new InvalidOperationException("Access to the service provider of the endpoint is only possible with in a When statement.");
            }

            if (!serviceProviders.TryGetValue(endpointName, out var serviceProvider))
            {
                throw new InvalidOperationException("Could not find service provider for endpoint " + endpointName);
            }

            return serviceProvider;
        }
    }

    public void RegisterServiceProvider(IServiceProvider serviceProvider, string endpointName) => serviceProviders[endpointName] = serviceProvider;

    readonly ConcurrentDictionary<string, IServiceProvider> serviceProviders = new();
}