using System;
using NServiceBus;
using NServiceBus.Persistence;

class Program
{
    static void Main()
    {
        var busConfiguration = new BusConfiguration();
        busConfiguration.EndpointName("SqlPersistenceSample");
        busConfiguration.UseSerialization<JsonSerializer>();
        busConfiguration.EnableInstallers();
        busConfiguration.UsePersistence<InMemoryPersistence>();
        var persistenceExtentions = busConfiguration.UsePersistence<SqlPersistence, StorageType.Timeouts>();
        persistenceExtentions
            .ConnectionString(@"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceSample;Integrated Security=True");

        using (var bus = Bus.Create(busConfiguration).Start())
        {
            bus.SendLocal(new StartOrder
            {
                OrderId = "123"
            });
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}