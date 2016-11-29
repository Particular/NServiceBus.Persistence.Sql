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

        var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencesample;Integrated Security=True";

        var sagaPersistence = configuration.UsePersistence<SqlPersistence, StorageType.Sagas>();
        sagaPersistence.ConnectionString(connectionString);

        var timeoutPersistence = configuration.UsePersistence<SqlPersistence, StorageType.Timeouts>();
        timeoutPersistence.ConnectionString(connectionString);

        var subscriptionPersistence = configuration.UsePersistence<SqlPersistence, StorageType.Subscriptions>();
        subscriptionPersistence.ConnectionString(connectionString);

        var outboxPersistence = configuration.UsePersistence<SqlPersistence, StorageType.Outbox>();
        outboxPersistence.ConnectionString(connectionString);

        return configuration;
    }
}