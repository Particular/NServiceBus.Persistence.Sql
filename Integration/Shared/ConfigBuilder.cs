using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;

public static class ConfigBuilder
{
    public static EndpointConfiguration Build(string enpointName)
    {
        var configuration = new EndpointConfiguration($"SqlPersistence.Sample{enpointName}");
        configuration.UseSerialization<JsonSerializer>();
        configuration.EnableInstallers();
        configuration.UsePersistence<InMemoryPersistence>();

        var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceSample;Integrated Security=True";

        var sagaPersistence = configuration.UsePersistence<SqlXmlPersistence, StorageType.Sagas>();
        sagaPersistence.ConnectionString(connectionString);

        var timeoutPersistence = configuration.UsePersistence<SqlXmlPersistence, StorageType.Timeouts>();
        timeoutPersistence.ConnectionString(connectionString);

        var subscriptionPersistence = configuration.UsePersistence<SqlXmlPersistence, StorageType.Subscriptions>();
        subscriptionPersistence.ConnectionString(connectionString);

        var outboxPersistence = configuration.UsePersistence<SqlXmlPersistence, StorageType.Outbox>();
        outboxPersistence.ConnectionString(connectionString);

        return configuration;
    }
}