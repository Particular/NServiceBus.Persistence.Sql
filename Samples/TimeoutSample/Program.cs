using System;
using NServiceBus;
using NServiceBus.Persistence;

class Program
{
    static void Main()
    {
        var busConfiguration = new BusConfiguration();
        busConfiguration.EndpointName("SqlPersistence.TimeoutSample");
        busConfiguration.UseSerialization<JsonSerializer>();
        busConfiguration.EnableInstallers();
        busConfiguration.UsePersistence<InMemoryPersistence>();
        var persistenceExtentions = busConfiguration.UsePersistence<SqlPersistence, StorageType.Timeouts>();
        persistenceExtentions
            .ConnectionString(@"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceSample;Integrated Security=True");

        using (var bus = Bus.Create(busConfiguration).Start())
        {
            var deferMessage = new DeferMessage {Property = "PropertyValue"};
            bus.Defer(TimeSpan.FromSeconds(2), deferMessage);
            bus.SendLocal(new StartSaga());
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}