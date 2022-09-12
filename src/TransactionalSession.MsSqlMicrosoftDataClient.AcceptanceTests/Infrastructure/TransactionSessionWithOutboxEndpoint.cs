namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;

    public class TransactionSessionWithOutboxEndpoint : TransactionSessionDefaultServer
    {
        public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor,
            EndpointCustomizationConfiguration endpointConfiguration,
            Action<EndpointConfiguration> configurationBuilderCustomization) =>
            base.GetConfiguration(runDescriptor, endpointConfiguration, configuration =>
            {
                configuration.ConfigureTransport().Transactions(TransportTransactionMode.ReceiveOnly);

                var outbox = configuration.EnableOutbox();
                outbox.DisableCleanup();

                configurationBuilderCustomization(configuration);
            });
    }
}