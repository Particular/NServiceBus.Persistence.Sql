namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using Configuration.AdvancedExtensibility;
    using Transport;

    public static class ConfigureExtensions
    {
        public static TransportExtensions ConfigureTransport(this EndpointConfiguration configuration) =>
            new TransportExtensions(configuration.GetSettings());

        public static TransportExtensions<TTransport> ConfigureTransport<TTransport>(
            this EndpointConfiguration configuration)
            where TTransport : TransportDefinition =>
            new TransportExtensions<TTransport>(configuration.GetSettings());
    }
}