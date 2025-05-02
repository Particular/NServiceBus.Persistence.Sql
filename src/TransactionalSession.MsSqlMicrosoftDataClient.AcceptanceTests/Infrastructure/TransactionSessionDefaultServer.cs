namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Configuration.AdvancedExtensibility;

    public class TransactionSessionDefaultServer : DefaultServer
    {
        public override async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomization, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            var endpointConfiguration = await base.GetConfiguration(runDescriptor, endpointCustomization, configurationBuilderCustomization);

            endpointConfiguration.GetSettings().Get<PersistenceExtensions<SqlPersistence>>()
                .EnableTransactionalSession();

            return endpointConfiguration;
        }
    }
}