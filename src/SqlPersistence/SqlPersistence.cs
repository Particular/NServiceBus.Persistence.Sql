namespace NServiceBus
{
    using Persistence;

    /// <summary>
    /// The <see cref="PersistenceDefinition"/> for the SQL Persistence.
    /// </summary>
    public partial class SqlPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<SqlPersistence>
    {
        // constructor parameter is a temporary workaround until the public constructor is removed
        SqlPersistence(object _)
        {
            Supports<StorageType.Outbox, SqlOutboxFeature>();
            Supports<StorageType.Sagas, SqlSagaFeature>();
            Supports<StorageType.Subscriptions, SqlSubscriptionFeature>();
        }

        static SqlPersistence IPersistenceDefinitionFactory<SqlPersistence>.Create() => new(null);
    }
}