using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;

public static class EndpointStarter
{
    public static async Task Start(string endpointName, Action<PersistenceExtensions<SqlPersistence>> configurePersistence)
    {
        var endpointConfiguration = new EndpointConfiguration($"SqlPersistence.Sample{endpointName}");
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.UseTransport<MsmqTransport>();
        endpointConfiguration.EnableOutbox();
        endpointConfiguration.SendFailedMessagesTo("Error");

        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        configurePersistence(persistence);

        var subscriptions = persistence.SubscriptionSettings();
        subscriptions.CacheFor(TimeSpan.FromMinutes(1));

        var endpoint = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);
        Console.WriteLine("Press any key to exit");
        var myEvent = new MyEvent
        {
            Property = "PropertyValue"
        };
        await endpoint.Publish(myEvent)
            .ConfigureAwait(false);

        var startSagaMessage = new StartSagaMessage
        {
            MySagaId = Guid.NewGuid()
        };
        await endpoint.SendLocal(startSagaMessage)
            .ConfigureAwait(false);

        var deferMessage = new DeferMessage
        {
            Property = "PropertyValue"
        };
        var options = new SendOptions();
        options.RouteToThisEndpoint();
        options.DelayDeliveryWith(TimeSpan.FromSeconds(1));
        await endpoint.Send(deferMessage, options)
            .ConfigureAwait(false);

        Console.ReadKey();
        await endpoint.Stop()
            .ConfigureAwait(false);
    }
}