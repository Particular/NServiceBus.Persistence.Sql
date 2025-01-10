namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.Threading.Tasks;
using AcceptanceTesting.Support;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

public class TransactionSessionWithSqlTransportOutboxEndpoint : TransactionSessionDefaultServer
{
    public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor,
        EndpointCustomizationConfiguration endpointConfiguration,
        Func<EndpointConfiguration, Task> configurationBuilderCustomization) =>
        base.GetConfiguration(runDescriptor, endpointConfiguration, async configuration =>
        {
            var transport = new SqlServerTransport(async cancellationToken =>
            {
                var conn = MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.Build("Transport");
                await conn.OpenAsync(cancellationToken);
                return conn;
            });

            configuration.UseTransport(transport);
            configuration.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

            var outbox = configuration.EnableOutbox();
            outbox.DisableCleanup();

            await configurationBuilderCustomization(configuration);
        });
}