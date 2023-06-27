namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Configuration.AdvancedExtensibility;

    public class NoPersistenceServer : IEndpointSetupTemplate
    {
        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor,
            EndpointCustomizationConfiguration endpointConfiguration,
#pragma warning disable PS0013
            Func<EndpointConfiguration, Task> configurationBuilderCustomization)
#pragma warning restore PS0013
        {
            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

            configuration.EnableInstallers();

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
            configuration.SendFailedMessagesTo("error");

            await configuration.DefineTransport(runDescriptor, endpointConfiguration).ConfigureAwait(false);

            configuration.GetSettings().SetDefault("ScaleOut.UseSingleBrokerQueue", true);
            await configurationBuilderCustomization(configuration).ConfigureAwait(false);

            configuration.ScanTypesForTest(endpointConfiguration);

            return configuration;
        }
    }
}
