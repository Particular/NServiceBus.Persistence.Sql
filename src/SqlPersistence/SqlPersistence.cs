namespace NServiceBus
{
    using Features;
    using Persistence;

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
            Defaults(s =>
            {
                var defaultsAppliedSettingsKey = "NServiceBus.Persistence.Sql.DefaultsApplied";

                if (s.HasSetting(defaultsAppliedSettingsKey))
                {
                    return;
                }

                var dialect = s.GetSqlDialect();
                var diagnostics = dialect.GetCustomDialectDiagnosticsInfo();

                s.AddStartupDiagnosticsSection("NServiceBus.Persistence.Sql.SqlDialect", new
                {
                    dialect.Name,
                    CustomDiagnostics = diagnostics
                });

                s.EnableFeatureByDefault<InstallerFeature>();

                s.Set(defaultsAppliedSettingsKey, true);
            });

            Supports<StorageType.Outbox>(s => s.EnableFeatureByDefault<SqlOutboxFeature>());
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<SqlSagaFeature>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<SqlSubscriptionFeature>());
        }
    }
}