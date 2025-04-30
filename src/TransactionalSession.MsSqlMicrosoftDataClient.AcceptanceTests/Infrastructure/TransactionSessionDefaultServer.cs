namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public partial class TransactionSessionDefaultServer : IEndpointSetupTemplate
    {
        public const string TransactionalSessionOptionsKey = "Test.TransactionalSessionOptions";
        public const string TablePrefixKey = "Test.TablePrefix";

        public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor,
            EndpointCustomizationConfiguration endpointConfiguration,
            Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);
            builder.UseSerialization<SystemJsonSerializer>();
            builder.EnableInstallers();

            builder.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));

            var storageDir = Path.Combine(Path.GetTempPath(), "learn", TestContext.CurrentContext.Test.ID);

            builder.UseTransport(new AcceptanceTestingTransport { StorageLocation = storageDir });

            var persistence = builder.UsePersistence<SqlPersistence>();
            SetConnectionBuilder(persistence);
            persistence.SqlDialect<SqlDialect.MsSqlServer>();
            persistence.DisableInstaller();

            if (this is not IDoNotCaptureServiceProvider)
            {
                builder.RegisterStartupTask(sp => new CaptureServiceProviderStartupTask(sp, runDescriptor.ScenarioContext));
            }

            await configurationBuilderCustomization(builder);

            if (!builder.GetSettings().TryGet<TransactionalSessionOptions>(TransactionalSessionOptionsKey, out var transactionalSessionOptions))
            {
                transactionalSessionOptions = new TransactionalSessionOptions();
            }

            if (builder.GetSettings().TryGet<string>(TablePrefixKey, out var tablePrefix))
            {
                persistence.TablePrefix(tablePrefix);
            }

            persistence.EnableTransactionalSession(transactionalSessionOptions);

            // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
            builder.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());

            return builder;
        }

        private partial void SetConnectionBuilder(PersistenceExtensions<SqlPersistence> sqlPersistence);
    }
}