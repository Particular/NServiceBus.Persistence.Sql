namespace NServiceBus.Persistence.Sql;

using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Features;
using NServiceBus.Sagas;

class ManifestOutput : Feature
{
    public ManifestOutput() => EnableByDefault();

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;

        if (!settings.GetOrDefault<bool>("Manifest.Enable"))
        {
            return;
        }

        _ = settings.TryGet<string>("NServiceBus.Routing.EndpointName", out var endpointName);

        var usingOutbox = settings.TryGet<FeatureState>("SqlOutboxFeature", out var outbox) && outbox == FeatureState.Active;
        var usingSqlSubscription = settings.TryGet<FeatureState>("SqlSubscriptionFeature", out var subscription) && subscription == FeatureState.Active;

        Dictionary<string, ManifestItems.ManifestItem> persistenceValues = [];
        //dialect information
        var dialect = settings.GetSqlDialect();
        persistenceValues.Add("dialect", (ManifestItems.ManifestItem)dialect.Name);

        //this applies to all queues/tables
        persistenceValues.Add("prefix", (ManifestItems.ManifestItem)endpointName);

        //outbox information
        if (usingOutbox)
        {
            persistenceValues.Add("outbox", new ManifestItems.ManifestItem { ItemValue = GetOutboxTableSchema() });

            //NOTE this is hardcoded so if the outbox script (Create_MsSqlServer.sql in ScriptBuilder/Outbox) changes this needs to be updated
            IEnumerable<KeyValuePair<string, ManifestItems.ManifestItem>> GetOutboxTableSchema()
            {
                return [
                    new("tableName", dialect.GetOutboxTableName($"{endpointName}_")),
                        new("primaryKey", "MessageId"),
                        new("indexes", new ManifestItems.ManifestItem { ArrayValue = [
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "Index_DispatchedAt"),
                                new("columns", "DispatchedAt")
                                ]},
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "Index_Dispatched"),
                                new("columns", "Dispatched")
                                ]}
                            ]
                        }),
                        new("tableColumns", new ManifestItems.ManifestItem { ArrayValue = [
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "MessageId"),
                                new("type", "string"),
                                new("length", "200"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "Dispatched"),
                                new("type", "boolean"),
                                new("mandatory", "true"),
                                new("default", "false")
                                ]},
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "DispatchedAt"),
                                new("type", "datetime"),
                                new("mandatory", "false")
                                ]},
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "PersistenceVersion"),
                                new("type", "string"),
                                new("length", "23"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItems.ManifestItem { ItemValue = [
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
            persistenceValues.Add("outbox", usingOutbox.ToString().ToLower());
        }

        //Saga information
        if (settings.TryGet($"NServiceBus.Sagas.SagaMetadataCollection", out SagaMetadataCollection sagas))
        {
            persistenceValues.Add("sagas", new ManifestItems.ManifestItem
            {
                ArrayValue = sagas.Select(
                        saga => new ManifestItems.ManifestItem
                        {
                            ItemValue = GetSagaTableSchema(saga.Name, saga.EntityName, saga.TryGetCorrelationProperty(out var correlationProperty) ? correlationProperty.Name : null)
                        }).ToArray()
            });

            //NOTE this is hardcoded so if the saga script (MsSqlServerSagaScriptWriter.cs in ScriptBuilder/Saga) changes this needs to be updated
            IEnumerable<KeyValuePair<string, ManifestItems.ManifestItem>> GetSagaTableSchema(string sagaName, string entityName, string correlationProperty)
            {
                return [
                    new("name", sagaName),
                        new("tableName", dialect.GetSagaTableName($"{endpointName}_", entityName)),
                        new("primaryKey", "Id"),
                        new("indexes", new ManifestItems.ManifestItem { ArrayValue = !string.IsNullOrEmpty(correlationProperty)
                            ? [
                                new ManifestItems.ManifestItem { ItemValue = [
                                    new("name", $"Index_Correlation_{correlationProperty}"),
                                    new("columns", correlationProperty)
                                    ]}
                                ]
                            : []
                        }),
                        new("tableColumns", new ManifestItems.ManifestItem { ArrayValue = [
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "Id"),
                                new("type", "guid"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "Metadata"),
                                new("type", "string"),
                                new("length", "max"),
                                new("mandatory", "true"),
                                ]},
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "Data"),
                                new("type", "string"),
                                new("length", "max"),
                                new("mandatory", "true"),
                                ]},

                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "PersistenceVersion"),
                                new("type", "string"),
                                new("length", "23"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "SagaTypeVersion"),
                                new("type", "string"),
                                new("length", "23"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItems.ManifestItem { ItemValue = [
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
            persistenceValues.Add("sqlSubscriptions", new ManifestItems.ManifestItem { ItemValue = GetSubscriptionTableSchema() });

            //NOTE this is hardcoded so if the subscription script (Create_MsSqlServer.sq in ScriptBuilder/Subscriptions) changes this needs to be updated
            IEnumerable<KeyValuePair<string, ManifestItems.ManifestItem>> GetSubscriptionTableSchema()
            {
                return [
                    new("tableName", dialect.GetSubscriptionTableName($"{endpointName}_")),
                        new("primaryKey", "Subscriber, MessageType"),
                        new("indexes", new ManifestItems.ManifestItem { ArrayValue = [] }),
                        new("tableColumns", new ManifestItems.ManifestItem { ArrayValue = [
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "Subscriber"),
                                new("type", "string"),
                                new("length", "200"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "Endpoint"),
                                new("type", "string"),
                                new("length", "200"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItems.ManifestItem { ItemValue = [
                                new("name", "MessageType"),
                                new("type", "string"),
                                new("length", "200"),
                                new("mandatory", "true")
                                ]},
                            new ManifestItems.ManifestItem { ItemValue = [
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
            persistenceValues.Add("sqlSubscriptions", usingSqlSubscription.ToString().ToLower());
        }

        ////connection information - include per storage type
        //var connectionString = string.Empty;
        //if (settings.TryGet($"SqlPersistence.ConnectionManager", out ConnectionManager connectionManager))
        //{
        //    connectionString = MaskPassword(connectionManager.BuildNonContextual().ConnectionString);
        //}
        //else
        //{
        //    Debug.WriteLine("No overall connection manager configured");
        //}

        if (settings.TryGet("ResultingSupportedStorages", out List<Type> supportedStorageTypes))
        {
            persistenceValues.Add(
                "storageTypes", new ManifestItems.ManifestItem
                {
                    ArrayValue = supportedStorageTypes.Select(
                        storageType => new ManifestItems.ManifestItem
                        {
                            ItemValue = [
                            new("type", storageType.Name),
                                //new("connection", GetConnectionString(storageType.Name))
                            ]
                        }).ToArray()
                });

            //string GetConnectionString(string storageTypeName)
            //{
            //    if (settings.TryGet($"SqlPersistence.ConnectionManager.{storageTypeName}", out ConnectionManager connectionManagerPerStorage))
            //    {
            //        return MaskPassword(connectionManagerPerStorage.BuildNonContextual().ConnectionString);
            //    }
            //    else
            //    {
            //        return connectionString;
            //    }
            //}
        }
        else
        {
            //persistenceValues.Add("connection", connectionString));
        }

        var manifest = settings.Get<ManifestItems>();
        manifest.Add("persistence", new ManifestItems.ManifestItem { ItemValue = persistenceValues });
    }

    //static string MaskPassword(string connectionString)
    //{
    //    if (string.IsNullOrEmpty(connectionString))
    //    {
    //        return connectionString;
    //    }

    //    // Regex matches Password=...; or Password=... (end of string)
    //    return System.Text.RegularExpressions.Regex.Replace(
    //        connectionString,
    //        @"(?i)(Password\s*=\s*)([^;]*)",
    //        "$1#####"
    //    );
    //}
}
