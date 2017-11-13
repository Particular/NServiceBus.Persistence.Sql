namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Configuration.AdvancedExtensibility;
    using Features;

    public class NoPersistenceServer : IEndpointSetupTemplate
    {
        public NoPersistenceServer()
        {
            typesToInclude = new List<Type>();
        }

        public NoPersistenceServer(List<Type> typesToInclude)
        {
            this.typesToInclude = typesToInclude;
        }

#pragma warning disable CS0618
        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
#pragma warning restore CS0618
        {
            var types = endpointConfiguration.GetTypesScopedByTestClass();

            typesToInclude.AddRange(types);

            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

            configuration.TypesToIncludeInScan(typesToInclude);
            configuration.EnableInstallers();

            configuration.DisableFeature<TimeoutManager>();

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
            configuration.SendFailedMessagesTo("error");

            await configuration.DefineTransport(runDescriptor, endpointConfiguration).ConfigureAwait(false);

            configuration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            configuration.GetSettings().SetDefault("ScaleOut.UseSingleBrokerQueue", true);
            configurationBuilderCustomization(configuration);

            return configuration;
        }

        List<Type> typesToInclude;
    }
}