using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;

public static class EndpointConfigurationExtensions
{

    public static List<Type> ScannedTypes(this EndpointConfiguration configuration)
    {
        var field = typeof(EndpointConfiguration)
            .GetField("scannedTypes", BindingFlags.Instance | BindingFlags.NonPublic);
        return (List<Type>)field.GetValue(configuration);
    }

    public static bool IsSendOnly(this EndpointConfiguration configuration)
    {
        return configuration.GetSettings().Get<bool>("Endpoint.SendOnly");
    }


    public static IEnumerable<Type> GetScannedSagaTypes(this EndpointConfiguration configuration)
    {
        return configuration.ScannedTypes()
            .Where(type => !type.IsAbstract && typeof(Saga).IsAssignableFrom(type));
    }
}