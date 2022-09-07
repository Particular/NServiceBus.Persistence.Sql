namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using Configuration.AdvancedExtensibility;
    using Transport;

    public static class ConfigureExtensions
    {
        public static TransportDefinition ConfigureTransport(this EndpointConfiguration configuration) =>
            configuration.GetSettings().Get<TransportDefinition>();

        public static TTransportDefinition ConfigureTransport<TTransportDefinition>(
            this EndpointConfiguration configuration)
            where TTransportDefinition : TransportDefinition =>
            (TTransportDefinition)configuration.GetSettings().Get<TransportDefinition>();
    }
}