using System;
using System.Linq;
using System.Reflection;
using System.Text;
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

    public async Task Cleanup()
    {
        using (var conn = PostgreSqlConnectionBuilder.Build())
        {
            conn.Open();

            var testingData = transport.GetType().GetProperty("Testing", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(transport);

            var queueAddresses = transport.ReceivingAddresses;

            var commandTextBuilder = new StringBuilder();

            //No clean-up for send-only endpoints
            if (queueAddresses != null)
            {
                foreach (var address in queueAddresses)
                {
                    commandTextBuilder.AppendLine($"DROP TABLE IF EXISTS {address};");
                }
            }

            //Null-check because if an exception is thrown before startup these fields might be empty
            if (testingData.GetType().GetProperty("DelayedDeliveryQueue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(testingData) is string delayedQueueAddress)
            {
                commandTextBuilder.AppendLine($"DROP TABLE IF EXISTS {delayedQueueAddress};");
            }

            var subscriptionTableName = testingData.GetType().GetProperty("SubscriptionTable", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(testingData) as string;

            if (!string.IsNullOrEmpty(subscriptionTableName))
            {
                commandTextBuilder.AppendLine($"DROP TABLE IF EXISTS {subscriptionTableName};");
            }

            var commandText = commandTextBuilder.ToString();
            if (!string.IsNullOrEmpty(commandText))
            {
                await TryDeleteTables(conn, commandText);
            }
        }
    }

    static async Task TryDeleteTables(NpgsqlConnection conn, string commandText)
    {
        try
        {
            using (var comm = conn.CreateCommand())
            {
                comm.CommandText = commandText;
                await comm.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            if (!e.Message.Contains("it does not exist or you do not have permission"))
            {
                throw;
            }
        }
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