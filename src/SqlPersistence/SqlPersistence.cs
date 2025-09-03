namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
            var persistenceName = ToString().Replace("NServiceBus.", "");
            _ = settings.TryGet<string>("NServiceBus.Routing.EndpointName", out var endpointName);

            var usingOutbox = settings.TryGet<FeatureState>("SqlOutboxFeature", out var outbox) && outbox == FeatureState.Active;
            var usingSqlSubscription = settings.TryGet<FeatureState>("SqlSubscriptionFeature", out var subscription) && subscription == FeatureState.Active;

            var persistenceValues = new List<KeyValuePair<string, ManifestItem>>();

            //dialect information
            if (settings.TryGet($"{persistenceName}.SqlDialect", out SqlDialectSettings dialectSettings))
            {
                persistenceValues.Add(new KeyValuePair<string, ManifestItem>("dialect", new ManifestItem { StringValue = dialectSettings.Dialect.Name }));
            }

            //this applies to all queues/tables
            persistenceValues.Add(new KeyValuePair<string, ManifestItem>("prefix", new ManifestItem { StringValue = endpointName }));

            //outbox information
            persistenceValues.Add(new KeyValuePair<string, ManifestItem>("usingOutbox", new ManifestItem { StringValue = usingOutbox.ToString() }));
            if (usingOutbox)
            {
                persistenceValues.Add(new KeyValuePair<string, ManifestItem>("outboxTable", new ManifestItem { StringValue = dialectSettings != null ? dialectSettings.Dialect.GetOutboxTableName($"{endpointName}_") : $"{endpointName}_OutboxData" }));
                //TODO table schema is not available in code
            }

            //sqlSubscription information
            persistenceValues.Add(new KeyValuePair<string, ManifestItem>("usingSqlSubscription", new ManifestItem { StringValue = usingSqlSubscription.ToString() }));
            if (usingSqlSubscription)
            {
                persistenceValues.Add(new KeyValuePair<string, ManifestItem>("outboxTable", new ManifestItem { StringValue = dialectSettings != null ? dialectSettings.Dialect.GetSubscriptionTableName($"{endpointName}_") : $"{endpointName}_SubscriptionData" }));
                //TODO table schema is not available in code
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
                        new("tableName", new ManifestItem { StringValue = dialectSettings !=null ?  dialectSettings.Dialect.GetSagaTableName($"{endpointName}_", saga.EntityName) : saga.EntityName }),
                        new("schema", new ManifestItem { ArrayValue = saga.SagaEntityType.GetProperties().Select(
                            prop => new ManifestItem { ItemValue = [
                                new("name", prop.Name),
                                new("type", prop.PropertyType.Name)
                                ]
                            }).ToArray() })
                        ]
                    }));
                }
            }

            //connection information - include per storage type
            var connectionString = string.Empty;
            if (settings.TryGet($"SqlPersistence.ConnectionManager", out ConnectionManager connectionManager))
            {
                connectionString = MaskPassword(connectionManager.BuildNonContextual().ConnectionString);
            }
            else
            {
                Debug.WriteLine("No overall connection manager configured");
            }

            if (settings.TryGet("ResultingSupportedStorages", out List<Type> supportedStorageTypes))
            {
                var storageManifest = new KeyValuePair<string, ManifestItem>(
                    "storageTypes", new ManifestItem
                    {
                        ArrayValue = supportedStorageTypes.Select(
                            storageType => new ManifestItem
                            {
                                ItemValue = [
                                new("type", storageType.Name),
                                new("connection", GetConnectionString(storageType.Name))
                                ]
                            }).ToArray()
                    });

                string GetConnectionString(string storageTypeName)
                {
                    if (settings.TryGet($"SqlPersistence.ConnectionManager.{storageTypeName}", out ConnectionManager connectionManagerPerStorage))
                    {
                        return MaskPassword(connectionManagerPerStorage.BuildNonContextual().ConnectionString);
                    }
                    else
                    {
                        return connectionString;
                    }
                }

                persistenceValues.Add(storageManifest);
            }
            else
            {
                persistenceValues.Add(new KeyValuePair<string, ManifestItem>("connection", new ManifestItem { StringValue = connectionString }));
            }

            //var installerSettings = settings.Get<InstallerSettings>("InstallerSettings");

            var persistenceManifest = new KeyValuePair<string, ManifestItem>(persistenceName, new ManifestItem() { ItemValue = persistenceValues.AsEnumerable() });

            return [persistenceManifest];
        }

        static string MaskPassword(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return connectionString;
            }

            // Regex matches Password=...; or Password=... (end of string)
            return System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(?i)(Password\s*=\s*)([^;]*)",
                "$1#####"
            );
        }
    }
}