using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;

public static class EndpointStarter
{
    public static async Task Start(string endpointName, Action<PersistenceExtensions<SqlPersistence>> configurePersistence)
    {
        var endpointConfiguration = new EndpointConfiguration($"SqlPersistence.Sample{endpointName}");
        endpointConfiguration.UseSerialization<JsonSerializer>();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.EnableOutbox();
        endpointConfiguration.SendFailedMessagesTo("Error");

        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        configurePersistence(persistence);

        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.CacheFor(TimeSpan.FromMinutes(1));

        var endpoint = await Endpoint.Start(endpointConfiguration);
        Console.WriteLine("Press any key to exit");
        try
        {
            var myEvent = new MyEvent
            {
                Property = "PropertyValue"
            };
            await endpoint.Publish(myEvent);

            var startSagaMessage = new StartSagaMessage
            {
                MySagaId = Guid.NewGuid()
            };
            await endpoint.SendLocal(startSagaMessage);

            var deferMessage = new DeferMessage
            {
                Property = "PropertyValue"
            };
            var options = new SendOptions();
            options.RouteToThisEndpoint();
            options.DelayDeliveryWith(TimeSpan.FromSeconds(1));
            await endpoint.Send(deferMessage, options);

            Console.ReadKey();
        }
        finally
        {
            await endpoint.Stop();
        }
    }
}