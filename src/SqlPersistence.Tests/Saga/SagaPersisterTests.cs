using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Sagas;
using NServiceBus.Transport;
using NUnit.Framework;
using Particular.Approvals;

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

    (SagaPersister, SagaInfoCache, SqlDialect) SetUp(string endpointName, string theSchema)
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
        return (new SagaPersister(infoCache, runtimeSqlDialect), infoCache, runtimeSqlDialect);
    }

    IEnumerable<Type> GetSagasAndFinders()
    {
        foreach (var nestedType in typeof(SagaPersisterTests).GetNestedTypes().Where(LoadTypeForSagaMetadata))
        {
            if (typeof(Saga).IsAssignableFrom(nestedType))
            {
                yield return nestedType;
            }
            if (nestedType.GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISagaFinder<,>)))
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
            connection.Open();
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

            connection.Open();

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
            Assert.That(exceptionMessage, Is.Not.Null, "Expected ExecuteCommand to throw");
            Assert.That(exceptionMessage, Does.Contain("Incorrect data type for Correlation_"));
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

            connection.Open();

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
            Assert.That(exceptionMessage, Is.Not.Null, "Expected ExecuteCommand to throw");
            Assert.That(exceptionMessage, Does.Contain("Incorrect data type for Correlation_"));
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

        var (persister, infoCache, dialect) = SetUp(endpointName, schema);

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            await persister.Save(sagaData, storageSession, "theProperty");
            await persister.Complete(sagaData, storageSession, 1);
            Assert.That((await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession).ConfigureAwait(false)).Data, Is.Null);
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

        var (persister, infoCache, dialect) = SetUp(endpointName, schema);

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            await persister.Save(sagaData, storageSession, "theProperty");

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
            return Task.CompletedTask;
        }
    }

    public class AMessage : IMessage
    {
        public string StringId { get; set; }
        public int IntId { get; set; }
    }

    [Test]
    public async Task Save()
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
        var result = await SaveAsync(id, endpointName);

        Assert.That(result, Is.Not.Null);
        Approver.Verify(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    void DropAndCreate(SagaDefinition definition, string endpointName, string theSchema)
    {
        using (var connection = GetConnection()(theSchema))
        {
            connection.Open();

            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition, sqlDialect), endpointName, schema: theSchema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition, sqlDialect), endpointName, schema: theSchema);
        }
    }

    [Test]
    public async Task CallbackIsInvoked()
    {
        var callbackInvoked = false;

        var (_, infoCache, dialect) = SetUp(nameof(CallbackIsInvoked), schema);

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            storageSession.OnSaveChanges((_, __) =>
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            });

            await storageSession.CompleteAsync();
        }

        Assert.That(callbackInvoked, Is.True);
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
            CorrelationProperty = "theCorrelationProperty",
        };

        var definition = new SagaDefinition(
            tableSuffix: "SagaWithCorrelation",
            name: "SagaWithCorrelation",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            ));

        DropAndCreate(definition, nameof(CallbackThrows), schema);

        var (persister, infoCache, dialect) = SetUp(nameof(CallbackThrows), schema);

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            await persister.Save(sagaData, storageSession, "theProperty");

            storageSession.OnSaveChanges((_, __) => throw new Exception("Simulated"));

            try
            {
                await storageSession.CompleteAsync();
            }
            catch (Exception)
            {
                exceptionThrown = true;
            }
        }

        Assert.That(exceptionThrown, Is.True);

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            var savedEntity = await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession);

            Assert.That(savedEntity.Data, Is.Null);
        }
    }

    async Task<SagaWithCorrelation.SagaData> SaveAsync(Guid id, string endpointName, CancellationToken cancellationToken = default)
    {
        var sagaData = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue",
            CorrelationProperty = "theCorrelationProperty"
        };

        var (persister, infoCache, dialect) = SetUp(endpointName, schema);

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null, cancellationToken);

            await persister.Save(sagaData, storageSession, "theProperty", cancellationToken);
            return (await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession, cancellationToken).ConfigureAwait(false)).Data;
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

            Assert.That(result, Is.Not.Null);
            Approver.Verify(result, s => s.Replace(id.ToString(), "theSagaId"));
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

    async Task<SagaWithWeirdCharactersಠ_ಠ.SagaData> SaveWeirdAsync(Guid id, string endpointName, CancellationToken cancellationToken = default)
    {
        var sagaData = new SagaWithWeirdCharactersಠ_ಠ.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageIdಠ_ಠ",
            Originator = "theOriginatorಠ_ಠ",
            SimplePropertyಠ_ಠ = "PropertyValueಠ_ಠ",
            Contentಠ_ಠ = "♟⛺"
        };

        var (persister, infoCache, dialect) = SetUp(endpointName, schema);

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null, cancellationToken);

            await persister.Save(sagaData, storageSession, "thePropertyಠ_ಠ", cancellationToken);
            return (await persister.Get<SagaWithWeirdCharactersಠ_ಠ.SagaData>(id, storageSession, cancellationToken).ConfigureAwait(false)).Data;
        }
    }

    [Test]
    public async Task SaveWithSpaceInName()
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
        var result = await SaveWithSpaceAsync(id, endpointName);

        Assert.That(result, Is.Not.Null);
        Approver.Verify(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<SagaWithSpaceInName.SagaData> SaveWithSpaceAsync(Guid id, string endpointName, CancellationToken cancellationToken = default)
    {
        var sagaData = new SagaWithSpaceInName.SagaData
        {
            Id = id,
            OriginalMessageId = "original message id",
            Originator = "the originator",
            SimpleProperty = "property value"
        };

        var (persister, infoCache, dialect) = SetUp(endpointName, schema);

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null, cancellationToken);

            await persister.Save(sagaData, storageSession, "property value", cancellationToken);
            return (await persister.Get<SagaWithSpaceInName.SagaData>(id, storageSession, cancellationToken).ConfigureAwait(false)).Data;
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
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task UpdateWithCorrectVersion()
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

        var (persister, infoCache, dialect) = SetUp(endpointName, schema);

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            await persister.Save(sagaData1, storageSession, "theProperty");
            await storageSession.CompleteAsync();
        }

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            var sagaData = await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession);
            sagaData.Data.SimpleProperty = "UpdatedValue";
            await persister.Update(sagaData.Data, storageSession, sagaData.Version);
            await storageSession.CompleteAsync();
        }

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            var sagaData = await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession);

            Assert.That(sagaData, Is.Not.EqualTo(default));
            Approver.Verify(sagaData, s => s.Replace(id.ToString(), "theSagaId"));
            Assert.That(sagaData.Version, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task UpdateWithTransitional()
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

        var (persister, infoCache, dialect) = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            await persister.Save(sagaData1, storageSession, "theProperty");
            await storageSession.CompleteAsync();
        }

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            var sagaData = await persister.Get<CorrAndTransitionalSaga.SagaData>(id, storageSession);
            sagaData.Data.SimpleProperty = "UpdatedValue";
            await persister.Update(sagaData.Data, storageSession, sagaData.Version);
            await storageSession.CompleteAsync();
        }

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            var sagaData = await persister.Get<CorrAndTransitionalSaga.SagaData>(id, storageSession);

            Assert.That(sagaData, Is.Not.EqualTo(default));
            Approver.Verify(sagaData, s => s.Replace(id.ToString(), "theSagaId"));
            Assert.That(sagaData.Version, Is.EqualTo(2));
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
            connection.Open();
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition1, sqlDialect), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition1, sqlDialect), endpointName, schema: schema);
            Assert.That(PropertyExists(TestTableName("TransitionalProcess", "CorrAndTransitionalSaga"), CorrelationPropertyName("Property1")), Is.True);

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
            Assert.Multiple(() =>
            {
                Assert.That(PropertyExists(TestTableName("TransitionalProcess", "CorrAndTransitionalSaga"), CorrelationPropertyName("Property1")), Is.True);
                Assert.That(PropertyExists(TestTableName("TransitionalProcess", "CorrAndTransitionalSaga"), CorrelationPropertyName("Property2")), Is.True);
            });

            var definition3 = new SagaDefinition(
                tableSuffix: "CorrAndTransitionalSaga",
                name: "CorrAndTransitionalSaga",
                correlationProperty: new CorrelationProperty
                (
                    name: "Property2",
                    type: CorrelationPropertyType.String
                )
            );
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition3, sqlDialect), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition3, sqlDialect), endpointName, schema: schema);
            Assert.Multiple(() =>
            {
                Assert.That(PropertyExists(TestTableName("TransitionalProcess", "CorrAndTransitionalSaga"), CorrelationPropertyName("Property1")), Is.False);
                Assert.That(PropertyExists(TestTableName("TransitionalProcess", "CorrAndTransitionalSaga"), CorrelationPropertyName("Property2")), Is.True);
            });
        }
    }

    protected virtual string CorrelationPropertyName(string propertyName) => $"Correlation_{propertyName}";

    protected virtual string TestTableName(string testName, string tableSuffix) => $"{testName}_{tableSuffix}";

    bool PropertyExists(string table, string propertyName)
    {
        using (var connection = GetConnection()(schema))
        {
            connection.Open();

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

        var (persister, infoCache, dialect) = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            await persister.Save(sagaData1, storageSession, "theProperty");
            await storageSession.CompleteAsync();
        }

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            var sagaData = await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession);
            sagaData.Data.SimpleProperty = "UpdatedValue";

            var exception = Assert.ThrowsAsync<Exception>(() => persister.Update(sagaData.Data, storageSession, wrongVersion));
            Assert.That(exception.Message, Does.Contain("Optimistic concurrency violation"));
        }
    }

    [Test]
    public async Task GetById()
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
        var result = await GetByIdAsync(id, endpointName);

        Assert.That(result, Is.Not.Null);
        Approver.Verify(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<SagaWithCorrelation.SagaData> GetByIdAsync(Guid id, string endpointName, CancellationToken cancellationToken = default)
    {
        var sagaData = new SagaWithCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "theSimpleProperty",
            CorrelationProperty = "theCorrelationProperty"
        };

        var (persister, infoCache, dialect) = SetUp(endpointName, schema);

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null, cancellationToken);

            await persister.Save(sagaData, storageSession, "theCorrelationProperty", cancellationToken);
            return (await persister.Get<SagaWithCorrelation.SagaData>(id, storageSession, cancellationToken).ConfigureAwait(false)).Data;
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
            return Task.CompletedTask;
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
            return Task.CompletedTask;
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
    public async Task GetByStringMapping()
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
        var result = await GetByStringMappingAsync(id, endpointName);

        Assert.That(result, Is.Not.Null);
        Approver.Verify(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<SagaWithStringCorrelation.SagaData> GetByStringMappingAsync(Guid id, string endpointName, CancellationToken cancellationToken = default)
    {
        var sagaData = new SagaWithStringCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = "theCorrelationProperty"
        };

        var (persister, infoCache, dialect) = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null, cancellationToken);

            await persister.Save(sagaData, storageSession, "theCorrelationProperty", cancellationToken);
            return (await persister.Get<SagaWithStringCorrelation.SagaData>("CorrelationProperty", "theCorrelationProperty", storageSession, cancellationToken).ConfigureAwait(false)).Data;
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
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task GetByNonStringMapping()
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
        var result = await GetByNonStringMappingAsync(id, endpointName);

        Assert.That(result, Is.Not.Null);
        Approver.Verify(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<NonStringCorrelationSaga.SagaData> GetByNonStringMappingAsync(Guid id, string endpointName, CancellationToken cancellationToken = default)
    {
        var sagaData = new NonStringCorrelationSaga.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            CorrelationProperty = 10
        };
        var (persister, infoCache, dialect) = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null, cancellationToken);

            await persister.Save(sagaData, storageSession, 666, cancellationToken);
            return (await persister.Get<NonStringCorrelationSaga.SagaData>("CorrelationProperty", 666, storageSession, cancellationToken).ConfigureAwait(false)).Data;
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
            return Task.CompletedTask;
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

        var (persister, infoCache, dialect) = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

            var data = new SagaWithCorrelation.SagaData
            {
                Id = Guid.NewGuid(),
                OriginalMessageId = "theOriginalMessageId",
                Originator = "theOriginator",
                CorrelationProperty = "theCorrelationProperty",
                SimpleProperty = "theSimpleProperty"
            };
            await persister.Save(data, storageSession, "theCorrelationProperty");
            await storageSession.CompleteAsync();
        }

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);

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
                await persister.Save(data, storageSession, "theCorrelationProperty");
                await storageSession.CompleteAsync();
            });
            var innerException = throwsAsync.InnerException;
            Assert.That(IsConcurrencyException(innerException), Is.True);
        }
    }

    [Test]
    public async Task SaveWithNoCorrelation()
    {
        var endpointName = nameof(SaveWithNoCorrelation);
        var definition = new SagaDefinition(
            tableSuffix: "SagaWithNoCorrelation",
            name: "SagaWithNoCorrelation"
        );
        DropAndCreate(definition, endpointName, schema);

        var id = Guid.NewGuid();
        var result = await SaveWithNoCorrelationAsync(id, endpointName);

        Assert.That(result, Is.Not.Null);
        Approver.Verify(result, s => s.Replace(id.ToString(), "theSagaId"));
    }

    async Task<SagaWithNoCorrelation.SagaData> SaveWithNoCorrelationAsync(Guid id, string endpointName, CancellationToken cancellationToken = default)
    {
        var sagaData = new SagaWithNoCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue",
        };

        var (persister, infoCache, dialect) = SetUp(endpointName, schema);
        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null, cancellationToken);

            await persister.Save(sagaData, storageSession, null, cancellationToken);
            return (await persister.Get<SagaWithNoCorrelation.SagaData>(id, storageSession, cancellationToken)).Data;
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
            return Task.CompletedTask;
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

        var (schemaPersister, schemaInfoCache, schemaDialect) = SetUp(endpointName, schema);
        var (defaultSchemaPersister, defaultSchemaInfoCache, defaultSchemaDialect) = SetUp(endpointName, null);

        var sagaData = new SagaWithNoCorrelation.SagaData
        {
            Id = id,
            OriginalMessageId = "theOriginalMessageId",
            Originator = "theOriginator",
            SimpleProperty = "PropertyValue",
        };

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), defaultSchemaInfoCache,
                   defaultSchemaDialect))
        {
            await storageSession.Open(null);

            await defaultSchemaPersister.Save(sagaData, storageSession, null);
        }

        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), schemaInfoCache,
                   schemaDialect))
        {
            await storageSession.Open(null);

            var result = (await schemaPersister.Get<SagaWithNoCorrelation.SagaData>(id, storageSession).ConfigureAwait(false)).Data;
            Assert.That(result, Is.Null);
        }
    }

    public class CustomFinder : ISagaFinder<SagaWithNoCorrelation.SagaData, AMessage>
    {
        public Task<SagaWithNoCorrelation.SagaData> FindBy(AMessage message, ISynchronizedStorageSession session, IReadOnlyContextBag context, CancellationToken cancellationToken = default) => null;
    }

    protected abstract bool IsConcurrencyException(Exception innerException);

    protected virtual bool SupportsUnicodeIdentifiers { get; } = true;

    protected virtual bool LoadTypeForSagaMetadata(Type type) => true;

    class FakeConnectionManager : IConnectionManager
    {
        readonly DbConnection connection;

        public FakeConnectionManager(DbConnection connection) => this.connection = connection;
        public DbConnection BuildNonContextual() => connection;

        public DbConnection Build(IncomingMessage incomingMessage) => connection;
    }
}