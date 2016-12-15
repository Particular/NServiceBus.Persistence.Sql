using System;
using System.Data.Common;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using ObjectApproval;

public abstract class SagaPersisterTests
{
    BuildSqlVarient sqlVarient;
    static string endpointName = "Endpoint";
    SagaPersister persister;
    Func<DbConnection> dbConnection;
    protected abstract Func<DbConnection> GetConnection();

    public SagaPersisterTests(BuildSqlVarient sqlVarient)
    {
        this.sqlVarient = sqlVarient;
        dbConnection = GetConnection();
    }

    [SetUp]
    public void SetUp()
    {
        var commandBuilder = new SagaCommandBuilder($"{endpointName}_");
        var infoCache = new SagaInfoCache(
            versionSpecificSettings: null, 
            jsonSerializer: Serializer.JsonSerializer, 
            commandBuilder: commandBuilder,
            readerCreator: reader => new JsonTextReader(reader),
            writerCreator: writer => new JsonTextWriter(writer));
        persister = new SagaPersister(infoCache);
    }

    [Test]
    public async Task Complete()
    {
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            ),
            transitionalCorrelationProperty: new CorrelationProperty
            (
                name: "TransitionalCorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        var dropScript = SagaScriptBuilder.BuildDropScript(definition, sqlVarient);
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVarient), endpointName);
        }
        var id = Guid.NewGuid();
        var sagaData = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = "theCorrelationProperty",
            TransitionalCorrelationProperty = "theTransitionalCorrelationProperty"
        };

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData, storageSession, typeof(SagaWithCorrelation), "theProperty");
            await persister.Complete(sagaData, storageSession, typeof(SagaWithCorrelation), 1);
            Assert.IsNull((await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession, typeof(SagaWithCorrelation))).Data);
        }
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
        }
    }

    [Test]
    public void SaveWithNoCorrelation()
    {
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithNoCorrelation",
            name: "SagaWithNoCorrelation"
        );
        var dropScript = SagaScriptBuilder.BuildDropScript(definition, sqlVarient);
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVarient), endpointName);
        }
        var id = Guid.NewGuid();
        var result = SaveWithNoCorrelationAsync(id).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));

        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
        }
    }

    async Task<SagaWithNoCorrelation.SagaData> SaveWithNoCorrelationAsync(Guid id)
    {
        var sagaData = new SagaWithNoCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue",
        };

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData, storageSession, typeof(SagaWithNoCorrelation), null);
            return (await persister.Get<SagaWithNoCorrelation.SagaData>(id, storageSession, typeof(SagaWithNoCorrelation))).Data;
        }
    }
    [SqlSaga]
    public class SagaWithNoCorrelation : Saga<SagaWithNoCorrelation.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string SimpleProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }
    [Test]
    public void Save()
    {
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            ),
            transitionalCorrelationProperty: new CorrelationProperty
            (
                name: "TransitionalCorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        var dropScript = SagaScriptBuilder.BuildDropScript(definition, sqlVarient);
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVarient), endpointName);
        }
        var id = Guid.NewGuid();
        var result = SaveAsync(id).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
        }
    }

    async Task<SagaWithCorrelation.SagaData> SaveAsync(Guid id)
    {
        var sagaData = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue",
            CorrelationProperty = "theCorrelationProperty",
            TransitionalCorrelationProperty = "theTransitionalCorrelationProperty"
        };

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData, storageSession, typeof(SagaWithCorrelation), "theProperty");
            return (await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession, typeof(SagaWithCorrelation))).Data;
        }
    }

    [Test]
    public void UpdateWithCorrectVersion()
    {
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            ),
            transitionalCorrelationProperty: new CorrelationProperty
            (
                name: "TransitionalCorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        var dropScript = SagaScriptBuilder.BuildDropScript(definition, sqlVarient);
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVarient), endpointName);
        }
        var id = Guid.NewGuid();
        var sagaData1 = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue",
            CorrelationProperty = "theCorrelationProperty",
            TransitionalCorrelationProperty = "theTransitionalCorrelationProperty"
        };

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            persister.Save(sagaData1, storageSession, typeof(SagaWithCorrelation), "theProperty").GetAwaiter().GetResult();
            storageSession.CompleteAsync().GetAwaiter().GetResult();
        }

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            var sagaData = persister.Get<SagaWithCorrelation.SagaData>(id, storageSession, typeof(SagaWithCorrelation)).GetAwaiter().GetResult();
            sagaData.Data.SimpleProperty = "UpdatedValue";
            persister.Update(sagaData.Data, storageSession, typeof(SagaWithCorrelation), sagaData.Version).GetAwaiter().GetResult();
            storageSession.CompleteAsync().GetAwaiter().GetResult();
        }

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            var sagaData = persister.Get<SagaWithCorrelation.SagaData>(id, storageSession, typeof(SagaWithCorrelation)).GetAwaiter().GetResult();
            ObjectApprover.VerifyWithJson(sagaData, s => s.Replace(id.ToString(), "theSagaId"));
            Assert.AreEqual(2, sagaData.Version);
        }

        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
        }
    }

    [Test]
    public void UpdateWithWrongVersion()
    {
        var wrongVersion = 666;

        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            ),
            transitionalCorrelationProperty: new CorrelationProperty
            (
                name: "TransitionalCorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        var dropScript = SagaScriptBuilder.BuildDropScript(definition, sqlVarient);
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVarient), endpointName);
        }
        var id = Guid.NewGuid();
        var sagaData1 = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue",
            CorrelationProperty = "theCorrelationProperty",
            TransitionalCorrelationProperty = "theTransitionalCorrelationProperty"
        };

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            persister.Save(sagaData1, storageSession, typeof(SagaWithCorrelation), "theProperty").GetAwaiter().GetResult();
            storageSession.CompleteAsync().GetAwaiter().GetResult();
        }

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            var sagaData = persister.Get<SagaWithCorrelation.SagaData>(id, storageSession, typeof(SagaWithCorrelation)).GetAwaiter().GetResult();
            sagaData.Data.SimpleProperty = "UpdatedValue";

            Assert.That(() => persister.Update(sagaData.Data, storageSession, typeof(SagaWithCorrelation), wrongVersion).GetAwaiter().GetResult(),
                Throws.Exception.Message.Contains("Optimistic concurrency violation"));
        }
    }


    [Test]
    public void GetById()
    {
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            ),
            transitionalCorrelationProperty: new CorrelationProperty
            (
                name: "TransitionalCorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        var dropScript = SagaScriptBuilder.BuildDropScript(definition, sqlVarient);
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVarient), endpointName);
        }
        var id = Guid.NewGuid();
        var result = GetByIdAsync(id).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
        }
    }

    async Task<SagaWithCorrelation.SagaData> GetByIdAsync(Guid id)
    {
        var sagaData = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "theSimpleProperty",
            CorrelationProperty = "theCorrelationProperty",
            TransitionalCorrelationProperty = "theTransitionalCorrelationProperty"
        };

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData, storageSession, typeof(SagaWithCorrelation), "theCorrelationProperty");
            return (await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession, typeof(SagaWithCorrelation))).Data;
        }
    }

    [SqlSaga(
         correlationProperty: nameof(SagaData.CorrelationProperty),
         transitionalCorrelationProperty: nameof(SagaData.TransitionalCorrelationProperty)
     )]
    public class SagaWithCorrelation : Saga<SagaWithCorrelation.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
            public string TransitionalCorrelationProperty { get; set; }
            public string SimpleProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }
    }


    [Test]
    public void GetByMapping()
    {
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            ),
            transitionalCorrelationProperty: new CorrelationProperty
            (
                name: "TransitionalCorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        var dropScript = SagaScriptBuilder.BuildDropScript(definition, sqlVarient);
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVarient), endpointName);
        }
        var id = Guid.NewGuid();
        var result = GetByMappingAsync(id).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
        }
    }

    async Task<SagaWithCorrelation.SagaData> GetByMappingAsync(Guid id)
    {
        var sagaData = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = "theCorrelationProperty",
            TransitionalCorrelationProperty = "theTransitionalCorrelationProperty",
            SimpleProperty = "theSimpleProperty"
        };
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData, storageSession, typeof(SagaWithCorrelation), "theCorrelationProperty");
            return (await persister.Get<SagaWithCorrelation.SagaData>("CorrelationProperty", "theCorrelationProperty", storageSession, typeof(SagaWithCorrelation))).Data;
        }
    }

    [Test]
    public async Task SaveDuplicateShouldThrow()
    {
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            ),
            transitionalCorrelationProperty: new CorrelationProperty
            (
                name: "TransitionalCorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        var dropScript = SagaScriptBuilder.BuildDropScript(definition, sqlVarient);
        using (var connection1 = dbConnection())
        {
            connection1.ExecuteCommand(dropScript, endpointName);
            connection1.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVarient), endpointName);
        }
        var sagaData1 = new SagaWithCorrelation.SagaData
        {
            Id = Guid.NewGuid(),
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = "theCorrelationProperty",
            TransitionalCorrelationProperty = "theTransitionalCorrelationProperty",
            SimpleProperty = "theSimpleProperty"
        };
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true))
        {
            await persister.Save(sagaData1, storageSession, typeof(SagaWithCorrelation), "theCorrelationProperty");
            var sagaData2 = new SagaWithCorrelation.SagaData
            {
                Id = Guid.NewGuid(),
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                CorrelationProperty = "theCorrelationProperty",
                TransitionalCorrelationProperty = "theTransitionalCorrelationProperty",
                SimpleProperty = "theSimpleProperty"
            };
            var throwsAsync = Assert.ThrowsAsync<Exception>(async () =>
            {
                await persister.Save(sagaData2, storageSession, typeof(SagaWithCorrelation), "theCorrelationProperty");
                await storageSession.CompleteAsync();
            });
            var innerException = throwsAsync.InnerException;
            Assert.IsTrue(IsConcurrencyException(innerException));
        }
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(dropScript, endpointName);
        }
    }

    protected abstract bool IsConcurrencyException(Exception innerException);
}