using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureEndpointPostgreSqlTransport : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        transport = new PostgreSqlTransport(async cancellationToken =>
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


            var commandTextBuilder = new StringBuilder();

            //No clean-up for send-only endpoints
            if (testingData.GetType().GetProperty("ReceiveAddresses", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(testingData) is string[] queueAddresses)
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

    PostgreSqlTransport transport;
}