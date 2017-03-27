using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Sagas;
using NUnit.Framework;
using ObjectApproval;
using NServiceBus.Extensibility;
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo

public abstract class SagaPersisterTests
{
    BuildSqlVariant sqlVariant;
    string schema;
    Func<DbConnection> dbConnection;
    protected abstract Func<DbConnection> GetConnection();

    public SagaPersisterTests(BuildSqlVariant sqlVariant, string schema)
    {
        this.sqlVariant = sqlVariant;
        this.schema = schema;
        dbConnection = GetConnection();
    }


    SagaPersister SetUp(string endpointName)
    {
#pragma warning disable 618
        var commandBuilder = new SagaCommandBuilder();
#pragma warning restore 618

        var sagaMetadataCollection = new SagaMetadataCollection();
        sagaMetadataCollection.Initialize(GetSagasAndFinders());

        var infoCache = new SagaInfoCache(
            versionSpecificSettings: null,
            jsonSerializer: Serializer.JsonSerializer,
            commandBuilder: commandBuilder,
            readerCreator: reader => new JsonTextReader(reader),
            writerCreator: writer => new JsonTextWriter(writer),
            tablePrefix: $"{endpointName}_",
            schema: schema,
            sqlVariant: sqlVariant.Convert(), 
            metadataCollection: sagaMetadataCollection);
        return new SagaPersister(infoCache);
    }

    IEnumerable<Type> GetSagasAndFinders()
    {
        foreach (var nestedType in typeof(SagaPersisterTests).GetNestedTypes())
        {
            if (typeof(Saga).IsAssignableFrom(nestedType))
            {
                yield return nestedType;
            }
            if (nestedType.GetInterfaces().Any(_=>_.Name.StartsWith("Using")))
            {
                yield return nestedType;
            }
            
        }
    }

    [Test]
    public async Task Complete()
    {
        var endpointName = nameof(Complete);
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
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVariant), endpointName, schema: schema);
        }
        var id = Guid.NewGuid();
        var sagaData = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = "theCorrelationProperty"
        };

        var persister = SetUp(endpointName);

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "theProperty");
            await persister.Complete(sagaData, storageSession, 1);
            Assert.IsNull((await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession)).Data);
        }
    }

    public class SagaWithWeirdCharactersಠ_ಠ : 
        SqlSaga<SagaWithWeirdCharactersಠ_ಠ.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string SimplePropertyಠ_ಠ { get; set; }
            public string Contentಠ_ಠ { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.SimplePropertyಠ_ಠ);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<AMessage>(_=>_.StringId);
        }

        public Task Handle(AMessage message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }
    }

    public class AMessage:IMessage
    {
        public string StringId { get; set; }
        public int IntId { get; set; }
    }

    [Test]
    public void Save()
    {
        var endpointName = nameof(Save);
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVariant), endpointName, schema: schema);
        }
        var id = Guid.NewGuid();
        var result = SaveAsync(id, endpointName).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<SagaWithCorrelation.SagaData> SaveAsync(Guid id, string endpointName)
    {
        var sagaData = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue",
            CorrelationProperty = "theCorrelationProperty"
        };

        var persister = SetUp(endpointName);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "theProperty");
            return (await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession)).Data;
        }
    }

    [Test]
    public virtual void SaveWithWeirdCharacters()
    {
        var endpointName = nameof(SaveWithWeirdCharacters);
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithWeirdCharactersಠ_ಠ",
            name: "SagaWithWeirdCharactersಠ_ಠ",
            correlationProperty: new CorrelationProperty
            (
                name: "SimplePropertyಠ_ಠ",
                type: CorrelationPropertyType.String
            )
        );
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVariant), endpointName, schema: schema);
        }
        var id = Guid.NewGuid();
        var result = SaveWeirdAsync(id, endpointName).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<SagaWithWeirdCharactersಠ_ಠ.SagaData> SaveWeirdAsync(Guid id, string endpointName)
    {
        var sagaData = new SagaWithWeirdCharactersಠ_ಠ.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageIdಠ_ಠ",
            Originator = "theOriginatorಠ_ಠ",
            SimplePropertyಠ_ಠ = "PropertyValueಠ_ಠ",
            Contentಠ_ಠ = "♟⛺"
        };

        var persister = SetUp(endpointName);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "thePropertyಠ_ಠ");
            return (await persister.Get<SagaWithWeirdCharactersಠ_ಠ.SagaData>(id, storageSession)).Data;
        }
    }

    [Test]
    public void UpdateWithCorrectVersion()
    {
        var endpointName = nameof(UpdateWithCorrectVersion);
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVariant), endpointName, schema: schema);
        }
        var id = Guid.NewGuid();
        var sagaData1 = new SagaWithCorrelation.SagaData
        {
            Id = id,
            CorrelationProperty = "theCorrelationProperty",
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue"
        };

        var persister = SetUp(endpointName);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            persister.Save(sagaData1, storageSession, "theProperty").GetAwaiter().GetResult();
            storageSession.CompleteAsync().GetAwaiter().GetResult();
        }

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            var sagaData = persister.Get<SagaWithCorrelation.SagaData>(id, storageSession).GetAwaiter().GetResult();
            sagaData.Data.SimpleProperty = "UpdatedValue";
            persister.Update(sagaData.Data, storageSession, sagaData.Version).GetAwaiter().GetResult();
            storageSession.CompleteAsync().GetAwaiter().GetResult();
        }

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            var sagaData = persister.Get<SagaWithCorrelation.SagaData>(id, storageSession).GetAwaiter().GetResult();
            ObjectApprover.VerifyWithJson(sagaData, s => s.Replace(id.ToString(), "theSagaId"));
            Assert.AreEqual(2, sagaData.Version);
        }
    }

    [Test]
    public async Task UpdateWithWrongVersion()
    {
        var wrongVersion = 666;
        var endpointName = nameof(UpdateWithWrongVersion);

        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVariant), endpointName, schema: schema);
        }
        var id = Guid.NewGuid();
        var sagaData1 = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue"
        };

        var persister = SetUp(endpointName);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData1, storageSession, "theProperty");
            await storageSession.CompleteAsync();
        }

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            var sagaData = await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession);
            sagaData.Data.SimpleProperty = "UpdatedValue";

            var exception = Assert.ThrowsAsync<Exception>(() => persister.Update(sagaData.Data, storageSession, wrongVersion));
            Assert.IsTrue(exception.Message.Contains("Optimistic concurrency violation"));
        }

    }


    [Test]
    public void GetById()
    {
        var endpointName = nameof(GetById);
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVariant), endpointName, schema: schema);
        }
        var id = Guid.NewGuid();
        var result = GetByIdAsync(id, endpointName).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<SagaWithCorrelation.SagaData> GetByIdAsync(Guid id, string endpointName)
    {
        var sagaData = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "theSimpleProperty",
            CorrelationProperty = "theCorrelationProperty"
        };

        var persister = SetUp(endpointName);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "theCorrelationProperty");
            return (await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession)).Data;
        }
    }

    [SqlSaga(
        TransitionalCorrelationProperty = nameof(SagaData.TransitionalCorrelationProperty)
    )]
    public class SagaWithCorrelationAndTransitional :
        SqlSaga<SagaWithCorrelationAndTransitional.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
            public string TransitionalCorrelationProperty { get; set; }
            public string SimpleProperty { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.CorrelationProperty);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<AMessage>(_ => _.StringId);
        }

        public Task Handle(AMessage message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }
    }

    public class SagaWithCorrelation :
        SqlSaga<SagaWithCorrelation.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
            public string SimpleProperty { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.CorrelationProperty);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<AMessage>(_ => _.StringId);
        }

        public Task Handle(AMessage message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }
    }

    [Test]
    [Explicit]
    public void TransitionId()
    {
        var endpointName = nameof(TransitionId);

        var definition1 = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition1, sqlVariant), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition1, sqlVariant), endpointName, schema: schema);

            var definition2 = new SagaDefinition(
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
                    type: CorrelationPropertyType.Guid
                )
            );
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition2, sqlVariant), endpointName, schema: schema);

            var definition3 = new SagaDefinition(
                tableSuffix: "SagaWithCorrelation",
                name: "SagaWithCorrelation",
                correlationProperty: new CorrelationProperty
                (
                    name: "TransitionalCorrelationProperty",
                    type: CorrelationPropertyType.Guid
                )
            );
            var buildCreateScript = SagaScriptBuilder.BuildCreateScript(definition3, sqlVariant);
            connection.ExecuteCommand(buildCreateScript, endpointName);
        }
    }

    [Test]
    public void GetByStringMapping()
    {
        var endpointName = nameof(GetByStringMapping);
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithStringCorrelation",
            name: "SagaWithStringCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVariant), endpointName, schema: schema);
        }
        var id = Guid.NewGuid();
        var result = GetByStringMappingAsync(id, endpointName).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<SagaWithStringCorrelation.SagaData> GetByStringMappingAsync(Guid id, string endpointName)
    {
        var sagaData = new SagaWithStringCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = "theCorrelationProperty"
        };
        var persister = SetUp(endpointName);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "theCorrelationProperty");
            return (await persister.Get<SagaWithStringCorrelation.SagaData>("CorrelationProperty", "theCorrelationProperty", storageSession)).Data;
        }
    }

    public class SagaWithStringCorrelation :
        SqlSaga<SagaWithStringCorrelation.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.CorrelationProperty);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<AMessage>(_ => _.StringId);
        }

        public Task Handle(AMessage message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }
    }

    [Test]
    public void GetByNonStringMapping()
    {
        var endpointName = nameof(GetByNonStringMapping);
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithNonStringCorrelation",
            name: "SagaWithNonStringCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.Int
            )
        );
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVariant), endpointName, schema: schema);
        }
        var id = Guid.NewGuid();
        var result = GetByNonStringMappingAsync(id, endpointName).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<SagaWithNonStringCorrelation.SagaData> GetByNonStringMappingAsync(Guid id, string endpointName)
    {
        var sagaData = new SagaWithNonStringCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = 10
        };
        var persister = SetUp(endpointName);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, 666);
            return (await persister.Get<SagaWithNonStringCorrelation.SagaData>("CorrelationProperty", 666, storageSession)).Data;
        }
    }

    public class SagaWithNonStringCorrelation :
        SqlSaga<SagaWithNonStringCorrelation.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public int CorrelationProperty { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.CorrelationProperty);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<AMessage>(_ => _.IntId);
        }

        public Task Handle(AMessage message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }
    }

    [Test]
    public async Task SaveDuplicateShouldThrow()
    {
        var endpointName = nameof(SaveDuplicateShouldThrow);

        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVariant), endpointName, schema: schema);
        }
        var sagaData1 = new SagaWithCorrelation.SagaData
        {
            Id = Guid.NewGuid(),
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = "theCorrelationProperty",
            SimpleProperty = "theSimpleProperty"
        };
        var persister = SetUp(endpointName);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData1, storageSession, "theCorrelationProperty");
            var sagaData2 = new SagaWithCorrelation.SagaData
            {
                Id = Guid.NewGuid(),
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                CorrelationProperty = "theCorrelationProperty",
                SimpleProperty = "theSimpleProperty"
            };
            var throwsAsync = Assert.ThrowsAsync<Exception>(async () =>
            {
                await persister.Save(sagaData2, storageSession, "theCorrelationProperty");
                await storageSession.CompleteAsync();
            });
            var innerException = throwsAsync.InnerException;
            Assert.IsTrue(IsConcurrencyException(innerException));
        }
    }


    [Test]
    public void SaveWithNoCorrelation()
    {
        var endpointName = nameof(SaveWithNoCorrelation);
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithNoCorrelation",
            name: "SagaWithNoCorrelation"
        );
        using (var connection = dbConnection())
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlVariant), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlVariant), endpointName, schema: schema);
        }
        var id = Guid.NewGuid();
        var result = SaveWithNoCorrelationAsync(id, endpointName).GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<SagaWithNoCorrelation.SagaData> SaveWithNoCorrelationAsync(Guid id, string endpointName)
    {
        var sagaData = new SagaWithNoCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue",
        };

        var persister = SetUp(endpointName);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, null);
            return (await persister.Get<SagaWithNoCorrelation.SagaData>(id, storageSession)).Data;
        }
    }

    public class SagaWithNoCorrelation :
        SqlSaga<SagaWithNoCorrelation.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string SimpleProperty { get; set; }
        }

        protected override string CorrelationPropertyName { get; }

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
        }

        public Task Handle(AMessage message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }
    }

    public class CustomFinder : IFindSagas<SagaWithNoCorrelation.SagaData>.Using<AMessage>
    {
        public Task<SagaWithNoCorrelation.SagaData> FindBy(AMessage message, SynchronizedStorageSession session, ReadOnlyContextBag context)
        {
            return null;
        }
    }

    protected abstract bool IsConcurrencyException(Exception innerException);
}