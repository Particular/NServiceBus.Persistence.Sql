namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;

    public class TransactionSessionWithOutboxEndpoint : TransactionSessionDefaultServer
    {
        public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor,
            EndpointCustomizationConfiguration endpointConfiguration,
            Func<EndpointConfiguration, Task> configurationBuilderCustomization) =>
            base.GetConfiguration(runDescriptor, endpointConfiguration, async configuration =>
            {
                configuration.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

                configuration.EnableOutbox()
                    .DisableCleanup();

                await configurationBuilderCustomization(configuration);
            });
    }
}