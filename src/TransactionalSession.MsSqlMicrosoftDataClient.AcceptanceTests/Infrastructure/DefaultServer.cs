namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.IO;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;
using Configuration.AdvancedExtensibility;
using NUnit.Framework;

public partial class DefaultServer : IEndpointSetupTemplate
{
    public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor,
        EndpointCustomizationConfiguration endpointCustomizationConfiguration,
        Func<EndpointConfiguration, Task> configurationBuilderCustomization)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointCustomizationConfiguration.EndpointName);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableInstallers();

        endpointConfiguration.Recoverability()
            .Delayed(delayed => delayed.NumberOfRetries(0))
            .Immediate(immediate => immediate.NumberOfRetries(0));

        var storageDir = Path.Combine(Path.GetTempPath(), "learn", TestContext.CurrentContext.Test.ID);

        endpointConfiguration.UseTransport(new AcceptanceTestingTransport { StorageLocation = storageDir });

        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        SetConnectionBuilder(persistence);
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        persistence.DisableInstaller();

        endpointConfiguration.GetSettings().Set(persistence);

        if (runDescriptor.ScenarioContext is TransactionalSessionTestContext testContext)
        {
            endpointConfiguration.RegisterStartupTask(sp => new CaptureServiceProviderStartupTask(sp, testContext, endpointCustomizationConfiguration.EndpointName));
        }

        await configurationBuilderCustomization(endpointConfiguration);

        // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
        endpointConfiguration.TypesToIncludeInScan(endpointCustomizationConfiguration.GetTypesScopedByTestClass());

        return endpointConfiguration;
    }

    private partial void SetConnectionBuilder(PersistenceExtensions<SqlPersistence> sqlPersistence);
}