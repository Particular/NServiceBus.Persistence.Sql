using NServiceBus;
using NServiceBus.Features;

static class EndpointConfigBuilder
{
    public static EndpointConfiguration BuildEndpoint(string s)
    {
        var endpointConfiguration = new EndpointConfiguration(s);
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.Recoverability().Immediate(c => c.NumberOfRetries(0));
        endpointConfiguration.Recoverability().Delayed(c => c.NumberOfRetries(0));
        return endpointConfiguration;
    }
}