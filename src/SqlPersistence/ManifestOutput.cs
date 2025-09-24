using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Features;
using NServiceBus.Sagas;

class ManifestOutput : Feature
{
    public ManifestOutput() => EnableByDefault();

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Used exclusively for serialization")]
    record PersistenceManifest
    {
        public string Dialect { get; init; }
        public string Prefix { get; init; }
        public OutboxManifest Outbox { get; set; }
        public SagaManifest[] Sagas { get; set; }
        public SubscriptionManifest SqlSubscriptions { get; set; }
        public string[] StorageTypes { get; set; }

        public record OutboxManifest
        {
            public string TableName { get; init; }
            //NOTE this is hardcoded so if the outbox script (Create_MsSqlServer.sql in ScriptBuilder/Outbox) changes this needs to be updated
            public string PrimaryKey => "MessageId";
            public IndexProperty[] Indexes => [
                new() { Name = "Index_DispatchedAt", Columns = "DispatchedAt" },
                new() { Name = "Index_Dispatched", Columns = "Dispatched" }
            ];
            public object[] TableColumns => [
                new VarcharSchemaProperty { Name = "MessageId", Length = "200", Mandatory = true },
                new BitSchemaProperty { Name = "Dispatched", Mandatory = true, Default = false },
                new SchemaProperty { Name = "DispatchedAt", Type = "datetime", Mandatory = false },
                new VarcharSchemaProperty { Name = "PersistenceVersion", Length = "23", Mandatory = true },
                new VarcharSchemaProperty { Name = "Operations", Length = "max", Mandatory = true }
            ];
        }

        public record SagaManifest
        {
            public string Name { get; init; }
            public string PrimaryKey => "Id";
            public string TableName { get; init; }
            public IndexProperty[] Indexes { get; init; }
            //NOTE this is hardcoded so if the saga script (MsSqlServerSagaScriptWriter.cs in ScriptBuilder/Saga) changes this needs to be updated
            public object[] TableColumns => [
                new SchemaProperty { Name = "Id", Type = "guid", Mandatory = true },
                new VarcharSchemaProperty { Name = "Metadata", Length = "max", Mandatory = true },
                new VarcharSchemaProperty { Name = "Data", Length = "max", Mandatory = true },
                new VarcharSchemaProperty { Name = "PersistenceVersion", Length = "23", Mandatory = true },
                new VarcharSchemaProperty { Name = "SagaTypeVersion", Length = "23", Mandatory = true },
                new SchemaProperty { Name = "Concurrency", Type = "integer", Mandatory = true }
            ];
        }

        public record SubscriptionManifest
        {
            public string TableName { get; init; }
            //NOTE this is hardcoded so if the subscription script (Create_MsSqlServer.sq in ScriptBuilder/Subscriptions) changes this needs to be updated
            public string PrimaryKey => "Subscriber, MessageType";
            public IndexProperty[] Indexes => [];
            public object[] TableColumns => [
                new VarcharSchemaProperty { Name = "Subscriber", Length = "200", Mandatory = true },
                new VarcharSchemaProperty { Name = "Endpoint", Length = "200", Mandatory = true },
                new VarcharSchemaProperty { Name = "MessageType", Length = "200", Mandatory = true },
                new VarcharSchemaProperty { Name = "PersistenceVersion", Length = "23", Mandatory = true }
            ];
        }

        public record SchemaProperty
        {
            public string Name { get; init; }
            public virtual string Type { get; set; }
            public bool Mandatory { get; init; }
        }

        public record VarcharSchemaProperty : SchemaProperty
        {
            public override string Type => "string";
            public string Length { get; init; }
        }

        public record BitSchemaProperty : SchemaProperty
        {
            public override string Type => "boolean";
            public bool Default { get; init; }
        }

        public record IndexProperty
        {
            public string Name { get; init; }
            public string Columns { get; init; }
        }
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        var endpointName = settings.Get<string>("NServiceBus.Routing.EndpointName");
        //dialect information
        if (!settings.HasSetting("SqlPersistence.SqlDialect"))
        {
            return;
        }
        var dialect = settings.GetSqlDialect();

        var manifest = new PersistenceManifest
        {
            Dialect = dialect.Name,
            //this applies to all queues/tables
            Prefix = endpointName
        };

        //outbox information
        if (settings.TryGet<FeatureState>("SqlOutboxFeature", out var outbox) && outbox == FeatureState.Active)
        {
            manifest.Outbox = new PersistenceManifest.OutboxManifest { TableName = dialect.GetOutboxTableName($"{endpointName}_") };
        }

        //Saga information
        if (settings.TryGet($"NServiceBus.Sagas.SagaMetadataCollection", out SagaMetadataCollection sagas))
        {
            manifest.Sagas = sagas.Select(
                        saga => GetSagaTableSchema(saga.Name, saga.EntityName, saga.TryGetCorrelationProperty(out var correlationProperty) ? correlationProperty.Name : null)).ToArray();

            PersistenceManifest.SagaManifest GetSagaTableSchema(string sagaName, string entityName, string correlationProperty) => new()
            {
                Name = sagaName,
                TableName = dialect.GetSagaTableName($"{endpointName}_", entityName),
                Indexes = !string.IsNullOrEmpty(correlationProperty)
                            ? [new() { Name = $"Index_Correlation_{correlationProperty}", Columns = correlationProperty }]
                            : []
            };
        }

        //sqlSubscription information
        if (settings.TryGet<FeatureState>("SqlSubscriptionFeature", out var subscription) && subscription == FeatureState.Active)
        {
            manifest.SqlSubscriptions = new PersistenceManifest.SubscriptionManifest
            {
                TableName = dialect.GetSubscriptionTableName($"{endpointName}_")
            };
        }

        if (settings.TryGet("ResultingSupportedStorages", out List<Type> supportedStorageTypes))
        {
            manifest.StorageTypes = supportedStorageTypes.Select(storageType => storageType.Name).ToArray();
        }

        settings.AddStartupDiagnosticsSection("Manifest-Persistence", manifest);
    }
}
