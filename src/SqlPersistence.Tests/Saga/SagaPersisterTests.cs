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
using NServiceBus.Extensibility;
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo

public abstract class SagaPersisterTests
{
    BuildSqlDialect sqlDialect;
    string schema;
    Func<DbConnection> dbConnection;
    protected abstract Func<string, DbConnection> GetConnection();
    protected abstract string GetPropertyWhereClauseExists(string schema, string table, string propertyName);
    protected virtual bool SupportsSchemas() => true;

    public SagaPersisterTests(BuildSqlDialect sqlDialect, string schema)
    {
        this.sqlDialect = sqlDialect;
        this.schema = schema;
        dbConnection = () => GetConnection()(schema);
    }

    SagaPersister SetUp(string endpointName, string theSchema)
    {
        var runtimeSqlDialect = sqlDialect.Convert(theSchema);

        var sagaMetadataCollection = new SagaMetadataCollection();
        sagaMetadataCollection.Initialize(GetSagasAndFinders());

        var infoCache = new SagaInfoCache(
            versionSpecificSettings: null,
            jsonSerializer: Serializer.JsonSerializer,
            readerCreator: reader => new JsonTextReader(reader),
            writerCreator: writer => new JsonTextWriter(writer),
            tablePrefix: $"{endpointName}_",
            sqlDialect: runtimeSqlDialect,
            metadataCollection: sagaMetadataCollection,
            nameFilter: sagaName => sagaName);
        return new SagaPersister(infoCache, runtimeSqlDialect);
    }

    IEnumerable<Type> GetSagasAndFinders()
    {
        foreach (var nestedType in typeof(SagaPersisterTests).GetNestedTypes().Where(LoadTypeForSagaMetadata))
        {
            if (typeof(Saga).IsAssignableFrom(nestedType))
            {
                yield return nestedType;
            }
            if (nestedType.GetInterfaces().Any(_ => _.Name.StartsWith("Using")))
            {
                yield return nestedType;
            }
        }
    }

    [Test]
    public void ExecuteCreateTwice()
    {
        var endpointName = nameof(ExecuteCreateTwice);
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
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlDialect), endpointName, schema: schema);
            var createScript = SagaScriptBuilder.BuildCreateScript(definition, sqlDialect);
            connection.ExecuteCommand(createScript, endpointName, schema: schema);
            connection.ExecuteCommand(createScript, endpointName, schema: schema);
        }
    }

    [Test]
    public void CreateWithDiffCorrelationType()
    {
        var endpointName = nameof(CreateWithDiffCorrelationType);
        using (var connection = dbConnection())
        {
            var definition1 = new SagaDefinition(
                tableSuffix: "SagaWithCorrelation",
                name: "SagaWithCorrelation",
                correlationProperty: new CorrelationProperty
                (
                    name: "CorrelationProperty",
                    type: CorrelationPropertyType.String
                )
            );
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition1, sqlDialect), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition1, sqlDialect), endpointName, schema: schema);
            var definition2 = new SagaDefinition(
                tableSuffix: "SagaWithCorrelation",
                name: "SagaWithCorrelation",
                correlationProperty: new CorrelationProperty
                (
                    name: "CorrelationProperty",
                    type: CorrelationPropertyType.DateTime
                )
            );
            var createScript2 = SagaScriptBuilder.BuildCreateScript(definition2, sqlDialect);

            string exceptionMessage = null;
            try
            {
                connection.ExecuteCommand(createScript2, endpointName, schema: schema);
            }
            catch (Exception exception)
            {
                exceptionMessage = exception.Message;
            }
            Assert.IsNotNull(exceptionMessage, "Expected ExecuteCommand to throw");
            StringAssert.Contains("Incorrect data type for Correlation_", exceptionMessage);
        }
    }

    [Test]
    public void CreateWithDiffTransType()
    {
        var endpointName = nameof(CreateWithDiffTransType);
        using (var connection = dbConnection())
        {
            var definition1 = new SagaDefinition(
                tableSuffix: "SagaWithCorrelation",
                name: "SagaWithCorrelation",
                correlationProperty: new CorrelationProperty
                (
                    name: "CorrelationProperty",
                    type: CorrelationPropertyType.String
                ),
                transitionalCorrelationProperty: new CorrelationProperty
                (
                    name: "TransCorrelationProperty",
                    type: CorrelationPropertyType.String
                )
            );
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition1, sqlDialect), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition1, sqlDialect), endpointName, schema: schema);
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
                    name: "TransCorrelationProperty",
                    type: CorrelationPropertyType.DateTime
                )
            );
            var createScript2 = SagaScriptBuilder.BuildCreateScript(definition2, sqlDialect);

            string exceptionMessage = null;
            try
            {
                connection.ExecuteCommand(createScript2, endpointName, schema: schema);
            }
            catch (Exception exception)
            {
                exceptionMessage = exception.Message;
            }
            Assert.IsNotNull(exceptionMessage, "Expected ExecuteCommand to throw");
            StringAssert.Contains("Incorrect data type for Correlation_", exceptionMessage);
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
        DropAndCreate(definition, endpointName, schema);
        var id = Guid.NewGuid();
        var sagaData = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = "theCorrelationProperty"
        };

        var persister = SetUp(endpointName, schema);

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "theProperty").ConfigureAwait(false);
            await persister.Complete(sagaData, storageSession, 1).ConfigureAwait(false);
            Assert.IsNull((await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession).ConfigureAwait(false)).Data);
        }
    }

    [Test]
    public async Task CompleteFailsOnConcurrencyViolation()
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
        DropAndCreate(definition, endpointName, schema);
        var id = Guid.NewGuid();
        var sagaData = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = "theCorrelationProperty"
        };

        var persister = SetUp(endpointName, schema);

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "theProperty").ConfigureAwait(false);

            const int invalidConcurrencyVersion = 42;
            Assert.ThrowsAsync<Exception>(() => persister.Complete(sagaData, storageSession, invalidConcurrencyVersion));
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
            mapper.ConfigureMapping<AMessage>(_ => _.StringId);
        }

        public Task Handle(AMessage message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }
    }

    public class AMessage : IMessage
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
        DropAndCreate(definition, endpointName, schema);
        var id = Guid.NewGuid();
        var result = SaveAsync(id, endpointName).GetAwaiter().GetResult();
        Assert.IsNotNull(result);
        TestApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    void DropAndCreate(SagaDefinition definition, string endpointName, string theSchema)
    {
        using (var connection = GetConnection()(theSchema))
        {
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlDialect), endpointName, schema: theSchema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlDialect), endpointName, schema: theSchema);
        }
    }

    [Test]
    public async Task CallbackIsInvoked()
    {
        var callbackInvoked = false;
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            storageSession.OnSaveChanges(s =>
            {
                callbackInvoked = true;
                return Task.FromResult(0);
            });
            await storageSession.CompleteAsync();
        }
        Assert.IsTrue(callbackInvoked);
    }

    [Test]
    public async Task CallbackThrows()
    {
        var exceptionThrown = false;
        var id = Guid.NewGuid();
        var sagaData = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue",
            CorrelationProperty = "theCorrelationProperty"
        };

        var persister = SetUp(nameof(CallbackThrows), schema);
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );
        DropAndCreate(definition, nameof(CallbackThrows), schema);

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "theProperty").ConfigureAwait(false);
            storageSession.OnSaveChanges(s =>
            {
                throw new Exception("Simulated");
            });
            try
            {
                await storageSession.CompleteAsync();
            }
            catch (Exception)
            {
                exceptionThrown = true;
            }
        }

        Assert.IsTrue(exceptionThrown);

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            var savedEntity = await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession).ConfigureAwait(false);
            Assert.IsNull(savedEntity.Data);
        }
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

        var persister = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "theProperty").ConfigureAwait(false);
            return (await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession).ConfigureAwait(false)).Data;
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

        var execute = new TestDelegate(() =>
        {
            DropAndCreate(definition, endpointName, schema);
            var id = Guid.NewGuid();
            var result = SaveWeirdAsync(id, endpointName).GetAwaiter().GetResult();
            Assert.IsNotNull(result);
            TestApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
        });

        if (SupportsUnicodeIdentifiers)
        {
            execute();
        }
        else
        {
            Assert.Throws<Exception>(execute);
        }
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

        var persister = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "thePropertyಠ_ಠ").ConfigureAwait(false);
            return (await persister.Get<SagaWithWeirdCharactersಠ_ಠ.SagaData>(id, storageSession).ConfigureAwait(false)).Data;
        }
    }

    [Test]
    public void SaveWithSpaceInName()
    {
        var endpointName = nameof(SaveWithSpaceInName);
        var definition = new SagaDefinition(
            tableSuffix: "SagaWith SpaceInName",
            name: nameof(SagaWithSpaceInName),
            correlationProperty: new CorrelationProperty
            (
                name: "SimpleProperty",
                type: CorrelationPropertyType.String
            )
        );

        DropAndCreate(definition, endpointName, schema);
        var id = Guid.NewGuid();
        var result = SaveWithSpaceAsync(id, endpointName).GetAwaiter().GetResult();
        Assert.IsNotNull(result);
        TestApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<SagaWithSpaceInName.SagaData> SaveWithSpaceAsync(Guid id, string endpointName)
    {
        var sagaData = new SagaWithSpaceInName.SagaData
        {
            Id = id,
            OriginalMessageId = "original message id",
            Originator = "the originator",
            SimpleProperty = "property value"
        };

        var persister = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "property value").ConfigureAwait(false);
            return (await persister.Get<SagaWithSpaceInName.SagaData>(id, storageSession).ConfigureAwait(false)).Data;
        }
    }

    public class SagaWithSpaceInName :
        SqlSaga<SagaWithSpaceInName.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string SimpleProperty { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.SimpleProperty);

        protected override string TableSuffix => "SagaWith SpaceInName";

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
        DropAndCreate(definition, endpointName, schema);
        var id = Guid.NewGuid();
        var sagaData1 = new SagaWithCorrelation.SagaData
        {
            Id = id,
            CorrelationProperty = "theCorrelationProperty",
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue"
        };

        var persister = SetUp(endpointName, schema);
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
            Assert.IsNotNull(sagaData);
            TestApprover.VerifyWithJson(sagaData, s => s.Replace(id.ToString(), "theSagaId"));
            Assert.AreEqual(2, sagaData.Version);
        }
    }

    [Test]
    public void UpdateWithTransitional()
    {
        var endpointName = nameof(UpdateWithTransitional);
        var definition = new SagaDefinition(
            tableSuffix: "CorrAndTransitionalSaga",
            name: "CorrAndTransitionalSaga",
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
        DropAndCreate(definition, endpointName, schema);
        var id = Guid.NewGuid();
        var sagaData1 = new CorrAndTransitionalSaga.SagaData
        {
            Id = id,
            CorrelationProperty = "theCorrelationProperty",
            TransitionalCorrelationProperty = "theTransitionalCorrelationProperty",
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue"
        };

        var persister = SetUp(endpointName, schema);
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
            var sagaData = persister.Get<CorrAndTransitionalSaga.SagaData>(id, storageSession).GetAwaiter().GetResult();
            sagaData.Data.SimpleProperty = "UpdatedValue";
            persister.Update(sagaData.Data, storageSession, sagaData.Version).GetAwaiter().GetResult();
            storageSession.CompleteAsync().GetAwaiter().GetResult();
        }

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            var sagaData = persister.Get<CorrAndTransitionalSaga.SagaData>(id, storageSession).GetAwaiter().GetResult();
            Assert.IsNotNull(sagaData);
            TestApprover.VerifyWithJson(sagaData, s => s.Replace(id.ToString(), "theSagaId"));
            Assert.AreEqual(2, sagaData.Version);
        }
    }

    [Test]
    public void TransitionalProcess()
    {
        var endpointName = nameof(TransitionalProcess);
        using (var connection = dbConnection())
        {
            var definition1 = new SagaDefinition(
                tableSuffix: "CorrAndTransitionalSaga",
                name: "CorrAndTransitionalSaga",
                correlationProperty: new CorrelationProperty
                (
                    name: "Property1",
                    type: CorrelationPropertyType.String
                )
            );
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition1, sqlDialect), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition1, sqlDialect), endpointName, schema: schema);
            Assert.IsTrue(PropertyExists(TestTableName("TransitionalProcess", "CorrAndTransitionalSaga"), CorrelationPropertyName("Property1")));

            var definition2 = new SagaDefinition(
                tableSuffix: "CorrAndTransitionalSaga",
                name: "CorrAndTransitionalSaga",
                correlationProperty: new CorrelationProperty
                (
                    name: "Property1",
                    type: CorrelationPropertyType.String
                ),
                transitionalCorrelationProperty: new CorrelationProperty
                (
                    name: "Property2",
                    type: CorrelationPropertyType.String
                )
            );

            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition2, sqlDialect), endpointName, schema: schema);
            Assert.IsTrue(PropertyExists(TestTableName("TransitionalProcess", "CorrAndTransitionalSaga"), CorrelationPropertyName("Property1")));
            Assert.IsTrue(PropertyExists(TestTableName("TransitionalProcess", "CorrAndTransitionalSaga"), CorrelationPropertyName("Property2")));


            var definition3 = new SagaDefinition(
                tableSuffix: "CorrAndTransitionalSaga",
                name: "CorrAndTransitionalSaga",
                correlationProperty: new CorrelationProperty
                (
                    name: "Property2",
                    type: CorrelationPropertyType.String
                )
            );
            var buildCreateScript = SagaScriptBuilder.BuildCreateScript(definition3, sqlDialect);
            connection.ExecuteCommand(buildCreateScript, endpointName, schema: schema);
            Assert.IsFalse(PropertyExists(TestTableName("TransitionalProcess", "CorrAndTransitionalSaga"), CorrelationPropertyName("Property1")));
            Assert.IsTrue(PropertyExists(TestTableName("TransitionalProcess", "CorrAndTransitionalSaga"), CorrelationPropertyName("Property2")));
        }
    }

    protected virtual string CorrelationPropertyName(string propertyName)
    {
        return $"Correlation_{propertyName}";
    }

    protected virtual string TestTableName(string testName, string tableSuffix)
    {
        return $"{testName}_{tableSuffix}";
    }

    bool PropertyExists(string table, string propertyName)
    {
        using (var connection = GetConnection()(schema))
        {
            var sql = GetPropertyWhereClauseExists(schema, table, propertyName);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        return false;
                    }
                    if (!reader.Read())
                    {
                        return false;
                    }
                    return reader.GetInt32(0) > 0;
                }
            }
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
        DropAndCreate(definition, endpointName, schema);
        var id = Guid.NewGuid();
        var sagaData1 = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue"
        };

        var persister = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData1, storageSession, "theProperty").ConfigureAwait(false);
            await storageSession.CompleteAsync().ConfigureAwait(false);
        }

        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            var sagaData = await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession).ConfigureAwait(false);
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
        DropAndCreate(definition, endpointName, schema);
        var id = Guid.NewGuid();
        var result = GetByIdAsync(id, endpointName).GetAwaiter().GetResult();
        Assert.IsNotNull(result);
        TestApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
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

        var persister = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "theCorrelationProperty").ConfigureAwait(false);
            return (await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession).ConfigureAwait(false)).Data;
        }
    }

    public class CorrAndTransitionalSaga :
        SqlSaga<CorrAndTransitionalSaga.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
            public string TransitionalCorrelationProperty { get; set; }
            public string SimpleProperty { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.CorrelationProperty);
        protected override string TransitionalCorrelationPropertyName => nameof(SagaData.TransitionalCorrelationProperty);

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
        using (var connection = dbConnection())
        {
            TransitionIdInner(connection);
        }
    }

    void TransitionIdInner(DbConnection connection)
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
        connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition1, sqlDialect), endpointName, schema: schema);
        connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition1, sqlDialect), endpointName, schema: schema);

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
        connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition2, sqlDialect), endpointName, schema: schema);

        var definition3 = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "TransitionalCorrelationProperty",
                type: CorrelationPropertyType.Guid
            )
        );
        var buildCreateScript = SagaScriptBuilder.BuildCreateScript(definition3, sqlDialect);
        connection.ExecuteCommand(buildCreateScript, endpointName);
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
        DropAndCreate(definition, endpointName, schema);
        var id = Guid.NewGuid();
        var result = GetByStringMappingAsync(id, endpointName).GetAwaiter().GetResult();
        Assert.IsNotNull(result);
        TestApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
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
        var persister = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, "theCorrelationProperty").ConfigureAwait(false);
            return (await persister.Get<SagaWithStringCorrelation.SagaData>("CorrelationProperty", "theCorrelationProperty", storageSession).ConfigureAwait(false)).Data;
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
            tableSuffix: "NonStringCorrelationSaga",
            name: "NonStringCorrelationSaga",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.Int
            )
        );
        DropAndCreate(definition, endpointName, schema);
        var id = Guid.NewGuid();
        var result = GetByNonStringMappingAsync(id, endpointName).GetAwaiter().GetResult();
        Assert.IsNotNull(result);
        TestApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<NonStringCorrelationSaga.SagaData> GetByNonStringMappingAsync(Guid id, string endpointName)
    {
        var sagaData = new NonStringCorrelationSaga.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = 10
        };
        var persister = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, 666).ConfigureAwait(false);
            return (await persister.Get<NonStringCorrelationSaga.SagaData>("CorrelationProperty", 666, storageSession).ConfigureAwait(false)).Data;
        }
    }

    public class NonStringCorrelationSaga :
        SqlSaga<NonStringCorrelationSaga.SagaData>,
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
        DropAndCreate(definition, endpointName, schema);
        var persister = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            var data = new SagaWithCorrelation.SagaData
            {
                Id = Guid.NewGuid(),
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                CorrelationProperty = "theCorrelationProperty",
                SimpleProperty = "theSimpleProperty"
            };
            await persister.Save(data, storageSession, "theCorrelationProperty").ConfigureAwait(false);
            await storageSession.CompleteAsync().ConfigureAwait(false);
        }
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            var data = new SagaWithCorrelation.SagaData
            {
                Id = Guid.NewGuid(),
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                CorrelationProperty = "theCorrelationProperty",
                SimpleProperty = "theSimpleProperty"
            };
            var throwsAsync = Assert.ThrowsAsync<Exception>(async () =>
            {
                await persister.Save(data, storageSession, "theCorrelationProperty").ConfigureAwait(false);
                await storageSession.CompleteAsync().ConfigureAwait(false);
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
        DropAndCreate(definition, endpointName, schema);
        var id = Guid.NewGuid();
        var result = SaveWithNoCorrelationAsync(id, endpointName).GetAwaiter().GetResult();
        Assert.IsNotNull(result);
        TestApprover.VerifyWithJson(result, s => s.Replace(id.ToString(), "theSagaId"));
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

        var persister = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, true, null))
        {
            await persister.Save(sagaData, storageSession, null).ConfigureAwait(false);
            return (await persister.Get<SagaWithNoCorrelation.SagaData>(id, storageSession).ConfigureAwait(false)).Data;
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

    [Test]
    public async Task UseConfiguredSchema()
    {
        if (!SupportsSchemas())
        {
            Assert.Ignore();
        }


        var endpointName = nameof(SaveWithNoCorrelation);
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithNoCorrelation",
            name: "SagaWithNoCorrelation"
        );
        DropAndCreate(definition, endpointName, null);
        DropAndCreate(definition, endpointName, schema);

        var id = Guid.NewGuid();

        var schemaPersister = SetUp(endpointName, schema);
        var defaultSchemaPersister = SetUp(endpointName, null);

        var sagaData = new SagaWithNoCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue",
        };

        using (var connection = GetConnection()(null))
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, false, null))
        {
            await defaultSchemaPersister.Save(sagaData, storageSession, null).ConfigureAwait(false);
        }

        using (var connection = GetConnection()(schema))
        using (var transaction = connection.BeginTransaction())
        using (var storageSession = new StorageSession(connection, transaction, false, null))
        {
            var result = (await schemaPersister.Get<SagaWithNoCorrelation.SagaData>(id, storageSession).ConfigureAwait(false)).Data;
            Assert.IsNull(result);
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

    protected virtual bool SupportsUnicodeIdentifiers { get; } = true;

    protected virtual bool LoadTypeForSagaMetadata(Type type)
    {
        return true;
    }
}