namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using NUnit.Framework;

    public partial class TransactionSessionDefaultServer : IEndpointSetupTemplate
    {
        public virtual Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration,
                                                                    Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);
            builder.EnableInstallers();

            builder.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));

            var transport = builder.UseTransport<AcceptanceTestingTransport>();
            transport.StorageDirectory(Path.Combine(Path.GetTempPath(), "learn", TestContext.CurrentContext.Test.ID));

            var persistence = builder.UsePersistence<SqlPersistence>();
            SetConnectionBuilder(persistence);
            persistence.SqlDialect<SqlDialect.MsSqlServer>();
            persistence.EnableTransactionalSession();

            builder.RegisterComponents(c => c.RegisterSingleton(runDescriptor.ScenarioContext)); // register base ScenarioContext type
            builder.RegisterComponents(c => c.RegisterSingleton(runDescriptor.ScenarioContext.GetType(), runDescriptor.ScenarioContext)); // register specific implementation

            endpointConfiguration.TypesToInclude.Add(typeof(CaptureBuilderFeature)); // required because the test assembly is excluded from scanning by default
            builder.EnableFeature<CaptureBuilderFeature>();

            configurationBuilderCustomization(builder);

            // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
            builder.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());

            return Task.FromResult(builder);
        }

        private partial void SetConnectionBuilder(PersistenceExtensions<SqlPersistence> sqlPersistence);
    }
}