using NServiceBus;
using NServiceBus.Persistence;

public static class ConfigBuilder
{
    public static BusConfiguration Build(string enpointName)
    {
        var configuration = new BusConfiguration();
        configuration.EndpointName("SqlPersistence.Sample" + enpointName);
        configuration.UseSerialization<JsonSerializer>();
        configuration.EnableInstallers();
        configuration.UsePersistence<InMemoryPersistence>();

        var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceSample;Integrated Security=True";
        
        var sagaPersistence = configuration.UsePersistence<SqlPersistence, StorageType.Sagas>();
        sagaPersistence.ConnectionString(connectionString);

        var timeoutPersistence = configuration.UsePersistence<SqlPersistence, StorageType.Timeouts>();
        timeoutPersistence.ConnectionString(connectionString);

        var subscriptionPersistence = configuration.UsePersistence<SqlPersistence, StorageType.Subscriptions>();
        subscriptionPersistence.ConnectionString(connectionString);

        return configuration;
    }
}