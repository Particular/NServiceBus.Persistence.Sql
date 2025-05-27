namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;

    public class TransactionSessionWithOutboxEndpoint : TransactionSessionDefaultServer
    {
        public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor,
            EndpointCustomizationConfiguration endpointCustomizationConfiguration,
            Func<EndpointConfiguration, Task> configurationBuilderCustomization) =>
            base.GetConfiguration(runDescriptor, endpointCustomizationConfiguration, async configuration =>
            {
                configuration.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

                configuration.EnableOutbox()
                    .DisableCleanup();

                await configurationBuilderCustomization(configuration);
            });
    }
}