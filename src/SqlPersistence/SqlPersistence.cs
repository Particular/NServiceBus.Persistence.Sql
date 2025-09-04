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
            if (usingOutbox)
            {
                persistenceValues.Add(new KeyValuePair<string, ManifestItem>("outbox", new ManifestItem { ItemValue = GetOutboxTableSchema() }));

                //NOTE this is hardcoded so if the outbox script (Create_MsSqlServer.sql in ScriptBuilder/Outbox) changes this needs to be updated
                IEnumerable<KeyValuePair<string, ManifestItem>> GetOutboxTableSchema()
                {
                    return [
                        new("tableName", new ManifestItem { StringValue = dialectSettings !=null ?  dialectSettings.Dialect.GetOutboxTableName($"{endpointName}_") : $"{endpointName}_OutboxData" }),
                        new("primaryKey", new ManifestItem { StringValue = "MessageId" }),
                        new("indexes", new ManifestItem { ArrayValue = [
                            new ManifestItem { ItemValue = [
                                new("name", "Index_DispatchedAt"),
                                new("columns", "DispatchedAt")
                                ]},
                            new ManifestItem { ItemValue = [
                                new("name", "Index_Dispatched"),
                                new("columns", "Dispatched")
                                ]}
                            ]
                        }),
                        new("tableColumns", new ManifestItem { ArrayValue = [
                            new ManifestItem { ItemValue = [
                                new("name", "MessageId"),
                                new("type", "string"),
                                new("length", "200"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItem { ItemValue = [
                                new("name", "Dispatched"),
                                new("type", "boolean"),
                                new("mandatory", "true"),
                                new("default", "false")
                                ]},
                            new ManifestItem { ItemValue = [
                                new("name", "DispatchedAt"),
                                new("type", "datetime"),
                                new("mandatory", "false")
                                ]},
                            new ManifestItem { ItemValue = [
                                new("name", "PersistenceVersion"),
                                new("type", "string"),
                                new("length", "23"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItem { ItemValue = [
                                new("name", "Operations"),
                                new("type", "string"),
                                new("length", "max"),
                                new("mandatory", "true")
                                ]},
                            ]
                        })
                    ];
                }
            }
            else
            {
                persistenceValues.Add(new KeyValuePair<string, ManifestItem>("outbox", new ManifestItem { StringValue = usingOutbox.ToString() }));
            }

            //Saga information
            if (settings.TryGet($"NServiceBus.Sagas.SagaMetadataCollection", out SagaMetadataCollection sagas))
            {
                var sagaManifests = new KeyValuePair<string, ManifestItem>("sagas", new ManifestItem
                {
                    ArrayValue = sagas.Select(
                            saga => new ManifestItem
                            {
                                ItemValue = GetSagaTableSchema(saga.Name, saga.EntityName, saga.TryGetCorrelationProperty(out var correlationProperty) ? correlationProperty.Name : null)
                            }).ToArray()
                });

                persistenceValues.Add(sagaManifests);

                //NOTE this is hardcoded so if the saga script (MsSqlServerSagaScriptWriter.cs in ScriptBuilder/Saga) changes this needs to be updated
                IEnumerable<KeyValuePair<string, ManifestItem>> GetSagaTableSchema(string sagaName, string entityName, string correlationProperty)
                {
                    return [
                        new("name", new ManifestItem { StringValue = sagaName }),
                        new("tableName", new ManifestItem { StringValue = dialectSettings !=null ?  dialectSettings.Dialect.GetSagaTableName($"{endpointName}_", entityName) : entityName }),
                        new("primaryKey", new ManifestItem { StringValue = "Id" }),
                        new("indexes", new ManifestItem { ArrayValue = !string.IsNullOrEmpty(correlationProperty)
                            ? [
                                new ManifestItem { ItemValue = [
                                    new("name", $"Index_Correlation_{correlationProperty}"),
                                    new("columns", correlationProperty)
                                    ]}
                                ]
                            : []
                        }),
                        new("tableColumns", new ManifestItem { ArrayValue = [
                            new ManifestItem { ItemValue = [
                                new("name", "Id"),
                                new("type", "guid"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItem { ItemValue = [
                                new("name", "Metadata"),
                                new("type", "string"),
                                new("length", "max"),
                                new("mandatory", "true"),
                                ]},
                            new ManifestItem { ItemValue = [
                                new("name", "Data"),
                                new("type", "string"),
                                new("length", "max"),
                                new("mandatory", "true"),
                                ]},

                            new ManifestItem { ItemValue = [
                                new("name", "PersistenceVersion"),
                                new("type", "string"),
                                new("length", "23"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItem { ItemValue = [
                                new("name", "SagaTypeVersion"),
                                new("type", "string"),
                                new("length", "23"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItem { ItemValue = [
                                new("name", "Concurrency"),
                                new("type", "integer"),
                                new("mandatory", "true")
                                ]},
                            ]
                        })
                    ];
                }
            }

            //sqlSubscription information
            if (usingSqlSubscription)
            {
                persistenceValues.Add(new KeyValuePair<string, ManifestItem>("sqlSubscriptions", new ManifestItem { ItemValue = GetSubscriptionTableSchema() }));

                //NOTE this is hardcoded so if the subscription script (Create_MsSqlServer.sq in ScriptBuilder/Subscriptions) changes this needs to be updated
                IEnumerable<KeyValuePair<string, ManifestItem>> GetSubscriptionTableSchema()
                {
                    return [
                        new("tableName", new ManifestItem { StringValue = dialectSettings != null ? dialectSettings.Dialect.GetSubscriptionTableName($"{endpointName}_") : $"{endpointName}_SubscriptionData" }),
                        new("primaryKey", new ManifestItem { StringValue = "Subscriber, MessageType" }),
                        new("indexes", new ManifestItem { ArrayValue = [] }),
                        new("tableColumns", new ManifestItem { ArrayValue = [
                            new ManifestItem { ItemValue = [
                                new("name", "Subscriber"),
                                new("type", "string"),
                                new("length", "200"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItem { ItemValue = [
                                new("name", "Endpoint"),
                                new("type", "string"),
                                new("length", "200"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItem { ItemValue = [
                                new("name", "MessageType"),
                                new("type", "string"),
                                new("length", "200"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItem { ItemValue = [
                                new("name", "PersistenceVersion"),
                                new("type", "string"),
                                new("length", "23"),
                                new("mandatory", "true")
                                ]}
                            ]
                        })
                    ];
                }
            }
            else
            {
                persistenceValues.Add(new KeyValuePair<string, ManifestItem>("sqlSubscriptions", new ManifestItem { StringValue = usingSqlSubscription.ToString() }));
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