using System;
using NServiceBus;
using NServiceBus.Persistence;

class Program
{
    static void Main()
    {
        var busConfiguration = new BusConfiguration();
        busConfiguration.EndpointName("SqlPersistence.PubSubSample");
        busConfiguration.UseSerialization<JsonSerializer>();
        busConfiguration.EnableInstallers();
        busConfiguration.UsePersistence<InMemoryPersistence>();
        var persistenceExtentions = busConfiguration.UsePersistence<SqlPersistence, StorageType.Subscriptions>();
        persistenceExtentions
            .ConnectionString(@"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceSample;Integrated Security=True");

        using (var bus = Bus.Create(busConfiguration).Start())
        {
            var myEvent = new MyEvent
            {
                Property = "PropertyValue"
            };
            bus.Publish(myEvent);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}