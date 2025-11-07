using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Settings;

class ManifestOutput : Feature
{
    public ManifestOutput() =>
        Defaults(s => s.Set(new PersistenceManifest { Prefix = s.Get<string>("NServiceBus.Routing.EndpointName") }));

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Used exclusively for serialization")]
    internal class PersistenceManifest
    {
        Lazy<OutboxManifest> outboxManifest;
        Lazy<SagaManifest[]> sagaManifests;
        Lazy<SubscriptionManifest> subscriptionManifest;

        public string Prefix { get; init; }
        public string Dialect { get; set; }
        public OutboxManifest Outbox => outboxManifest?.Value;
        public SagaManifest[] Sagas => sagaManifests?.Value;
        public SubscriptionManifest SqlSubscriptions => subscriptionManifest?.Value;
        public string[] StorageTypes { get; set; }

        //initialising lazy so that any reflection/calculations don't slow down endpoint startup
        public void SetOutbox(Func<OutboxManifest> lazyOutbox) => outboxManifest = new Lazy<OutboxManifest>(lazyOutbox);
        public void SetSagas(Func<SagaManifest[]> lazySagas) => sagaManifests = new Lazy<SagaManifest[]>(lazySagas);
        public void SetSqlSubscriptions(Func<SubscriptionManifest> lazySubscription) => subscriptionManifest = new Lazy<SubscriptionManifest>(lazySubscription);

        //NOTE Some values are hardcoded so if the outbox script (Create_MsSqlServer.sql in ScriptBuilder/Outbox) changes this needs to be updated
        public record OutboxManifest
        {
            public string TableName { get; init; }
            public string PrimaryKey => "MessageId";
            public IndexProperty[] Indexes => [
                new() { Name = "Index_DispatchedAt", Columns = "DispatchedAt" },
                new() { Name = "Index_Dispatched", Columns = "Dispatched" }
            ];
            //object to allow for serialization of multiple subtypes. They will never be deserialized
            public object[] TableColumns => [
                new VarcharSchemaProperty { Name = "MessageId", Length = "200", Mandatory = true },
                new BitSchemaProperty { Name = "Dispatched", Mandatory = true, Default = false },
                new SchemaProperty { Name = "DispatchedAt", Type = "datetime", Mandatory = false },
                new VarcharSchemaProperty { Name = "PersistenceVersion", Length = "23", Mandatory = true },
                new VarcharSchemaProperty { Name = "Operations", Length = "max", Mandatory = true }
            ];
        }

        //NOTE Some values are hardcoded so if the saga script (MsSqlServerSagaScriptWriter.cs in ScriptBuilder/Saga) changes this needs to be updated
        public record SagaManifest
        {
            public string Name { get; init; }
            public string PrimaryKey => "Id";
            public string TableName { get; init; }
            public IndexProperty[] Indexes { get; init; }
            //object to allow for serialization of multiple subtypes. They will never be deserialized
            public object[] TableColumns => [
                new SchemaProperty { Name = "Id", Type = "guid", Mandatory = true },
                new VarcharSchemaProperty { Name = "Metadata", Length = "max", Mandatory = true },
                new VarcharSchemaProperty { Name = "Data", Length = "max", Mandatory = true },
                new VarcharSchemaProperty { Name = "PersistenceVersion", Length = "23", Mandatory = true },
                new VarcharSchemaProperty { Name = "SagaTypeVersion", Length = "23", Mandatory = true },
                new SchemaProperty { Name = "Concurrency", Type = "integer", Mandatory = true }
            ];
        }

        //NOTE Some values are hardcoded so if the subscription script (Create_MsSqlServer.sq in ScriptBuilder/Subscriptions) changes this needs to be updated
        public record SubscriptionManifest
        {
            public string TableName { get; init; }
            public string PrimaryKey => "Subscriber, MessageType";
            public IndexProperty[] Indexes => [];
            //object to allow for serialization of multiple subtypes. They will never be deserialized
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
        if (!settings.HasSetting("SqlPersistence.SqlDialect"))
        {
            return;
        }

        settings.AddStartupDiagnosticsSection("Manifest-Persistence", () => GenerateManifest(context.Settings));
    }

    static PersistenceManifest GenerateManifest(IReadOnlySettings settings)
    {
        var manifest = settings.Get<PersistenceManifest>();
        manifest.Dialect = settings.GetSqlDialect().Name;

        if (settings.TryGet("ResultingSupportedStorages", out List<Type> supportedStorageTypes))
        {
            manifest.StorageTypes = supportedStorageTypes.Select(storageType => storageType.Name).ToArray();
        }

        return manifest;
    }
}
