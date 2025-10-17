namespace NServiceBus
{
    using Features;
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

            Supports<StorageType.Outbox, SqlOutboxFeature>();
            Supports<StorageType.Sagas, SqlSagaFeature>();
            Supports<StorageType.Subscriptions, SqlSubscriptionFeature>();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SqlPersistence"/> class.
        /// </summary>
        public static SqlPersistence Create() => new(null);
    }
}