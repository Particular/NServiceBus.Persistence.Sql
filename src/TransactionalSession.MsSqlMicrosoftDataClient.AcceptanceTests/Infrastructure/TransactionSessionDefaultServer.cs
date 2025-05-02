namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Configuration.AdvancedExtensibility;

    public class TransactionSessionDefaultServer : DefaultServer
    {
        public const string TransactionalSessionOptionsKey = "Test.TransactionalSessionOptions";

        public override async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomization, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            var endpointConfiguration = await base.GetConfiguration(runDescriptor, endpointCustomization, configurationBuilderCustomization);

            if (!endpointConfiguration.GetSettings().TryGet<TransactionalSessionOptions>(TransactionalSessionOptionsKey, out var transactionalSessionOptions))
            {
                transactionalSessionOptions = new TransactionalSessionOptions();
            }

            endpointConfiguration.GetSettings().Get<PersistenceExtensions<SqlPersistence>>()
                .EnableTransactionalSession(transactionalSessionOptions);

            return endpointConfiguration;
        }
    }
}