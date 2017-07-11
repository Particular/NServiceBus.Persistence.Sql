using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;

class Program
{
    static void Main()
    {
        Start().GetAwaiter().GetResult();
    }

    static async Task Start()
    {
        var connection = @"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencesample;Integrated Security=True";

        var endpointConfiguration = new EndpointConfiguration("SqlPersistence.Sample.PartialScripts");
        endpointConfiguration.UseSerialization<JsonSerializer>();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.EnableOutbox();
        endpointConfiguration.SendFailedMessagesTo("Error");

        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();

        persistence.SqlVariant(SqlVariant.MsSqlServer);
        persistence.TablePrefix("PartialScripts");
        persistence.ConnectionBuilder(() => new SqlConnection(connection));

        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Sagas>();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Outbox>();

        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.CacheFor(TimeSpan.FromMinutes(1));

        var endpoint = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);
        Console.WriteLine("Press any key to exit");
        try
        {
            var startSagaMessage = new StartSagaMessage
            {
                MySagaId = Guid.NewGuid()
            };
            await endpoint.SendLocal(startSagaMessage)
                .ConfigureAwait(false);
            Console.ReadKey();
        }
        finally
        {
            await endpoint.Stop()
                .ConfigureAwait(false);
        }


    }
}