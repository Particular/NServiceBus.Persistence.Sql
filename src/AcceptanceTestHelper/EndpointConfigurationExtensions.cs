using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;

public static class EndpointConfigurationExtensions
{
    public static List<Type> UserProvidedTypes(this EndpointConfiguration configuration)
    {
        var scannerConfigurationTypeName = "NServiceBus.AssemblyScanningComponent+Configuration";
        var userProvidedTypesPropertyName = "UserProvidedTypes";

        var scannerConfiguration = configuration.GetSettings().Get(scannerConfigurationTypeName);

        var property = scannerConfiguration.GetType().GetProperty(userProvidedTypesPropertyName);
        if (property == null)
        {
            throw new Exception($"Could not extract field '{userProvidedTypesPropertyName}' from {scannerConfigurationTypeName}.");
        }
        return (List<Type>)property.GetValue(scannerConfiguration);
    }

    public static bool IsSendOnly(this EndpointConfiguration configuration)
    {
        return configuration.GetSettings().Get<bool>("Endpoint.SendOnly");
    }

    public static IEnumerable<Type> GetScannedSagaTypes(this EndpointConfiguration configuration)
    {
        return configuration.UserProvidedTypes()
            .Where(type => !type.IsAbstract && typeof(Saga).IsAssignableFrom(type));
    }
}
