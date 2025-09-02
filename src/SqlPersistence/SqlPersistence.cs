namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Features;
    using NServiceBus.Sagas;
    using NServiceBus.Settings;
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

        /// <summary>
        /// Returns infrastructure information about the persistence.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<KeyValuePair<string, ManifestItem>> GetManifest(SettingsHolder settings)
        {
            var name = ToString().Replace("NServiceBus.", "");

            var persistenceValues = new List<KeyValuePair<string, ManifestItem>>();

            //dialect information
            if (settings.TryGet($"{name}.SqlDialect", out SqlDialectSettings dialectSettings))
            {
                //TODO can we somehow get connection details out of here?
                persistenceValues.Add(new KeyValuePair<string, ManifestItem>("dialect", new ManifestItem { StringValue = dialectSettings.Dialect.Name }));
                //dialectSettings.Dialect.GetOutboxTableName();
                Console.WriteLine(dialectSettings.GetType().GetGenericArguments()[0].Name);
            }

            //TODO difference connection managers - could be one for all storage types or one per storage type
            if (settings.TryGet($"SqlPersistence.ConnectionManager.{nameof(StorageType.Outbox)}", out ConnectionManager connectionManagerOutbox))
            {
                connectionManagerOutbox.ToString();
            }

            if (settings.TryGet($"SqlPersistence.ConnectionManager.{nameof(StorageType.Subscriptions)}", out ConnectionManager connectionManagerSubscription))
            {
                connectionManagerSubscription.ToString();
            }

            if (settings.TryGet($"SqlPersistence.ConnectionManager.{nameof(StorageType.Sagas)}", out ConnectionManager connectionManagerSaga))
            {
                connectionManagerSaga.ToString();
            }
            if (settings.TryGet($"SqlPersistence.ConnectionManager", out ConnectionManager connectionManager))
            {
                //TODO can we somehow get connection details out of here?
                connectionManager.ToString();
            }


            //Saga information
            if (settings.TryGet($"NServiceBus.Sagas.SagaMetadataCollection", out SagaMetadataCollection sagas))
            {
                sagas.ToString();
                foreach (var saga in sagas)
                {
                    persistenceValues.Add(new KeyValuePair<string, ManifestItem>($"saga-{saga.Name}", new ManifestItem
                    {
                        ItemValue = [
                        new("tableName", new ManifestItem { StringValue = saga.EntityName }),
                        new("schema", new ManifestItem { ArrayValue = saga.SagaEntityType.GetProperties().Select(
                            prop => new ManifestItem { ItemValue = [
                                new("name", prop.Name),
                                new("type", prop.PropertyType.Name)
                                ]
                            }).ToArray() })
                        ]
                    }));

                    Console.WriteLine(saga.Name);
                }
            }

            var persistenceManifest = new KeyValuePair<string, ManifestItem>(name, new ManifestItem() { ItemValue = persistenceValues.AsEnumerable() });

            return [persistenceManifest];
        }
    }
}