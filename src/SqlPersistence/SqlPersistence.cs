namespace NServiceBus.Persistence.Sql
{
    using Settings;
    using Features;

    /// <summary>
    /// The <see cref="PersistenceDefinition"/> for the SQL Persistence.
    /// </summary>
    public class SqlPersistence : PersistenceDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SqlPersistence"/>.
        /// </summary>
        public SqlPersistence()
        {
            Supports<StorageType.Outbox>(s =>
            {
                EnableSession(s);
                s.EnableFeatureByDefault<SqlOutboxFeature>();
            });
            Supports<StorageType.Timeouts>(s =>
            {
                s.EnableFeatureByDefault<SqlTimeoutFeature>();
            });
            Supports<StorageType.Sagas>(s =>
            {
                EnableSession(s);
                s.EnableFeatureByDefault<SqlSagaFeature>();
                s.AddUnrecoverableException(typeof(SerializationException));
            });
            Supports<StorageType.Subscriptions>(s =>
            {
                s.EnableFeatureByDefault<SqlSubscriptionFeature>();
            });
            Defaults(s =>
            {
                var dialect = s.GetSqlDialect();
                var diagnostics = dialect.GetCustomDialectDiagnosticsInfo();

                s.AddStartupDiagnosticsSection("NServiceBus.Persistence.Sql.SqlDialect", new
                {
                    Name = dialect.Name,
                    CustomDiagnostics = diagnostics
                });

                s.EnableFeatureByDefault<InstallerFeature>();
            });
        }

        static void EnableSession(SettingsHolder s)
        {
            s.EnableFeatureByDefault<StorageSessionFeature>();
        }
    }
}