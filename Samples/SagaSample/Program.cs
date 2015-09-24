using System;
using NServiceBus;
using NServiceBus.Persistence;

class Program
{
    static void Main()
    {
        var busConfiguration = new BusConfiguration();
        busConfiguration.EndpointName("SqlPersistence.SagaSample");
        busConfiguration.UseSerialization<JsonSerializer>();
        busConfiguration.EnableInstallers();
        busConfiguration.UsePersistence<InMemoryPersistence>();
        busConfiguration.UsePersistence<SqlPersistence, StorageType.Sagas>();
        var persistenceExtentions = busConfiguration.UsePersistence<SqlPersistence, StorageType.Timeouts>();
        persistenceExtentions
            .ConnectionString(@"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceSample;Integrated Security=True");

        using (var bus = Bus.Create(busConfiguration).Start())
        {
            bus.SendLocal(new StartSaga
            {
                MySagaId = "123"
            });
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}