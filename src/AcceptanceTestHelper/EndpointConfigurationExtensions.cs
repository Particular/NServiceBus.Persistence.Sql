using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;

public static class EndpointConfigurationExtensions
{
    public static bool IsSendOnly(this EndpointConfiguration configuration)
    {
        return configuration.GetSettings().Get<bool>("Endpoint.SendOnly");
    }
}