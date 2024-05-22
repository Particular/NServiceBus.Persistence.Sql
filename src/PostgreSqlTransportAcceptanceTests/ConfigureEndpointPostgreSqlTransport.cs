using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Transport;

public class ConfigureEndpointPostgreSqlTransport : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        transport = new TestingPostgreSqlTransport(async cancellationToken =>
        {
            var conn = PostgreSqlConnectionBuilder.Build();
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            return conn;
        });

        configuration.UseTransport(transport);

        return Task.CompletedTask;
    }

    public Task Cleanup()
    {
        using (var conn = PostgreSqlConnectionBuilder.Build())
        {
            conn.Open();

            //var queueAddresses = transport.ReceivingAddresses.ToList();
            //foreach (var address in queueAddresses)
            //{
            //    TryDeleteTable(conn, address);
            //    TryDeleteTable(conn, new QueueAddress(address.Table + ".Delayed", address.Schema, address.Catalog));
            //}
        }
        return Task.CompletedTask;
    }


    TestingPostgreSqlTransport transport;

    class TestingPostgreSqlTransport : PostgreSqlTransport
    {
        public TestingPostgreSqlTransport(string connectionString) : base(connectionString)
        {
        }

        public TestingPostgreSqlTransport(Func<CancellationToken, Task<NpgsqlConnection>> connectionFactory) : base(connectionFactory)
        {
        }

        public string[] ReceivingAddresses { get; private set; } = Array.Empty<string>();

        public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers,
            string[] sendingAddresses, CancellationToken cancellationToken = default)
        {
            var infra = await base.Initialize(hostSettings, receivers, sendingAddresses, cancellationToken);

            ReceivingAddresses = infra.Receivers.Select(r => r.Value.ReceiveAddress).ToArray();

            return infra;
        }
    }
}