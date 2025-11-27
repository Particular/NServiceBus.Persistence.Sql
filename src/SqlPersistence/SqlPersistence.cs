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
            Defaults(s =>
            {
                var dialect = s.GetSqlDialect();
                var diagnostics = dialect.GetCustomDialectDiagnosticsInfo();

                s.AddStartupDiagnosticsSection("NServiceBus.Persistence.Sql.SqlDialect", new
                {
                    dialect.Name,
                    CustomDiagnostics = diagnostics
                });
            });

            Supports<StorageType.Outbox, SqlOutboxFeature>();
            Supports<StorageType.Sagas, SqlSagaFeature>(new StorageType.SagasOptions { SupportsFinders = true });
            Supports<StorageType.Subscriptions, SqlSubscriptionFeature>();
        }

        static SqlPersistence IPersistenceDefinitionFactory<SqlPersistence>.Create() => new(null);
    }
}