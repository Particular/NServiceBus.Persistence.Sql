using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
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
        sagaMetadataCollection.AddRange(SagaMetadata.CreateMany(GetSagasAndFinders()));

        var infoCache = new SagaInfoCache(
            versionSpecificSettings: null,
            jsonSerializer: Serializer.JsonSerializer,
            readerCreator: reader => new JsonTextReader(reader),
            writerCreator: writer => new JsonTextWriter(writer),
            tablePrefix: $"{endpointName}_",
            sqlDialect: runtimeSqlDialect,
            metadataCollection: sagaMetadataCollection,
            nameFilter: sagaName => sagaName);
        return (new SagaPersister(infoCache, runtimeSqlDialect, new SqlTimeoutPersister(runtimeSqlDialect, $"{TimeoutEndpointName(endpointName)}_")), infoCache, runtimeSqlDialect);
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
            await Assert.ThatAsync(() => persister.Complete(sagaData, storageSession, invalidConcurrencyVersion), Throws.TypeOf<Exception>());
        }
    }

    public class SagaWithWeirdCharactersಠ_ಠ :
        Saga<SagaWithWeirdCharactersಠ_ಠ.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string SimplePropertyಠ_ಠ { get; set; }
            public string Contentಠ_ಠ { get; set; }
        }

        public Task Handle(AMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.SimplePropertyಠ_ಠ).ToMessage<AMessage>(m => m.StringId);
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
            connection.ExecuteCommand(SagaTimeoutScriptBuilder.BuildDropScript(sqlDialect), TimeoutEndpointName(endpointName), schema: theSchema);
            connection.ExecuteCommand(SagaTimeoutScriptBuilder.BuildCreateScript(sqlDialect), TimeoutEndpointName(endpointName), schema: theSchema);
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

        var execute = new Action(() =>
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

    [SqlSaga(tableSuffix: "SagaWith SpaceInName")]
    public class SagaWithSpaceInName :
        Saga<SagaWithSpaceInName.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string SimpleProperty { get; set; }
        }

        public Task Handle(AMessage message, IMessageHandlerContext context) => Task.CompletedTask;
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.SimpleProperty).ToMessage<AMessage>(m => m.StringId);
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

            Assert.That(sagaData, Is.Not.Default);
            Approver.Verify(sagaData, s => s.Replace(id.ToString(), "theSagaId"));
            Assert.That(sagaData.Version, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task InstallerCreatesTimeoutTable()
    {
        var endpointName = nameof(InstallerCreatesTimeoutTable);
        var (_, infoCache, dialect) = SetUp(endpointName, schema);
        var prefix = TimeoutEndpointName(endpointName);
        var timeoutPersister = new SqlTimeoutPersister(dialect, $"{prefix}_");
        var directory = Directory.CreateTempSubdirectory("sql-timeout-install-");
        try
        {
            using (var connection = dbConnection())
            {
                connection.Open();
                connection.ExecuteCommand(SagaTimeoutScriptBuilder.BuildDropScript(sqlDialect), prefix, schema: schema);
            }

            // An outbox-only script directory has no Sagas folder and needs no timeout script.
            await ScriptRunner.Install(dialect, $"{prefix}_", dbConnection, directory.FullName, shouldInstallOutbox: false, shouldInstallSubscriptions: false);

            Directory.CreateDirectory(Path.Combine(directory.FullName, "Sagas"));
            File.WriteAllText(Path.Combine(directory.FullName, "SagaTimeout_Create.sql"), SagaTimeoutScriptBuilder.BuildCreateScript(sqlDialect));
            await ScriptRunner.Install(dialect, $"{prefix}_", dbConnection, directory.FullName, shouldInstallOutbox: false, shouldInstallSubscriptions: false);

            await InSession(infoCache, dialect, async session =>
            {
                var saga = new SagaWithCorrelation.SagaData { Id = Guid.NewGuid() };
                var timeout = CreateStoredTimeout("installed", DateTimeOffset.UtcNow);
                await timeoutPersister.Request(saga, timeout, session, new ContextBag());
                AssertTimeoutsAreEqual((await timeoutPersister.Get(saga, session, new ContextBag())).Single(), timeout);
            });
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Test]
    public async Task RequestRescheduleCancelAndMarkFiredTimeouts()
    {
        var endpointName = nameof(RequestRescheduleCancelAndMarkFiredTimeouts);
        CreateTimeoutSchema(endpointName);
        var saga = new SagaWithCorrelation.SagaData { Id = Guid.NewGuid() };
        var first = CreateStoredTimeout("first", new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)).AddTicks(1234));
        var second = CreateStoredTimeout("second", new DateTimeOffset(2031, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var third = CreateStoredTimeout("third", new DateTimeOffset(2032, 1, 2, 3, 4, 5, TimeSpan.FromHours(-5)));
        var (_, infoCache, dialect) = SetUp(endpointName, schema);
        var timeoutPersister = new SqlTimeoutPersister(dialect, $"{TimeoutEndpointName(endpointName)}_");

        await InSession(infoCache, dialect, async session =>
        {
            await timeoutPersister.Request(saga, first, session, new ContextBag());
            await timeoutPersister.Request(saga, second, session, new ContextBag());
            await timeoutPersister.Request(saga, third, session, new ContextBag());
        });

        await InSession(infoCache, dialect, async session =>
        {
            var timeouts = await timeoutPersister.Get(saga, session, new ContextBag());

            Assert.That(timeouts, Has.Count.EqualTo(3));
            AssertTimeoutsAreEqual(timeouts.Single(t => t.TimeoutId == first.TimeoutId), first);
            AssertTimeoutsAreEqual(timeouts.Single(t => t.TimeoutId == second.TimeoutId), second);
            AssertTimeoutsAreEqual(timeouts.Single(t => t.TimeoutId == third.TimeoutId), third);
        });

        first.DueTime = new DateTimeOffset(2040, 6, 7, 8, 9, 10, TimeSpan.FromHours(10)).AddTicks(7);
        first.TriggerId = "rescheduled";
        first.Version = 2;
        first.Payload = [4, 5, 6];

        await InSession(infoCache, dialect, async session =>
        {
            await timeoutPersister.Reschedule(saga, first, session, new ContextBag());
            await timeoutPersister.Cancel(saga, second.TimeoutId, session, new ContextBag());
            await timeoutPersister.MarkFired(saga, third.TimeoutId, session, new ContextBag());
        });

        await InSession(infoCache, dialect, async session =>
        {
            var timeouts = await timeoutPersister.Get(saga, session, new ContextBag());

            Assert.That(timeouts, Has.Count.EqualTo(1));
            AssertTimeoutsAreEqual(timeouts[0], first);
            await timeoutPersister.Cancel(saga, second.TimeoutId, session, new ContextBag());
            await timeoutPersister.MarkFired(saga, third.TimeoutId, session, new ContextBag());
            await Assert.ThatAsync(() => timeoutPersister.Reschedule(saga, second, session, new ContextBag()), Throws.TypeOf<Exception>().With.Message.Contains("does not exist"));
        });
    }

    [Test]
    public async Task TimeoutsAreScopedBySagaAndRolledBack()
    {
        var endpointName = nameof(TimeoutsAreScopedBySagaAndRolledBack);
        CreateTimeoutSchema(endpointName);
        var saga1 = new SagaWithCorrelation.SagaData { Id = Guid.NewGuid() };
        var saga2 = new SagaWithCorrelation.SagaData { Id = Guid.NewGuid() };
        var timeout1 = CreateStoredTimeout("one", DateTimeOffset.UtcNow);
        var timeout2 = CreateStoredTimeout("two", DateTimeOffset.UtcNow);
        timeout2.TimeoutId = timeout1.TimeoutId;
        var (_, infoCache, dialect) = SetUp(endpointName, schema);
        var timeoutPersister = new SqlTimeoutPersister(dialect, $"{TimeoutEndpointName(endpointName)}_");

        await InSession(infoCache, dialect, async session =>
        {
            await timeoutPersister.Request(saga1, timeout1, session, new ContextBag());
            await timeoutPersister.Request(saga2, timeout2, session, new ContextBag());
        });
        await InSession(infoCache, dialect, async session =>
        {
            await timeoutPersister.Cancel(saga1, timeout1.TimeoutId, session, new ContextBag());
            await timeoutPersister.Request(saga1, CreateStoredTimeout("rolled back", DateTimeOffset.UtcNow), session, new ContextBag());
        }, commit: false);

        await InSession(infoCache, dialect, async session =>
        {
            var timeouts1 = await timeoutPersister.Get(saga1, session, new ContextBag());
            var timeouts2 = await timeoutPersister.Get(saga2, session, new ContextBag());

            Assert.That(timeouts1, Has.Count.EqualTo(1));
            AssertTimeoutsAreEqual(timeouts1[0], timeout1);
            Assert.That(timeouts2, Has.Count.EqualTo(1));
            AssertTimeoutsAreEqual(timeouts2[0], timeout2);
        });
    }

    [Test]
    public async Task CompleteRemovesAllTimeoutsOfTheSaga()
    {
        var endpointName = nameof(CompleteRemovesAllTimeoutsOfTheSaga);
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
        var saga = new SagaWithCorrelation.SagaData { Id = Guid.NewGuid(), CorrelationProperty = "completed" };
        var otherSaga = new SagaWithCorrelation.SagaData { Id = Guid.NewGuid(), CorrelationProperty = "other" };
        var (persister, infoCache, dialect) = SetUp(endpointName, schema);
        var timeoutPersister = new SqlTimeoutPersister(dialect, $"{TimeoutEndpointName(endpointName)}_");

        await InSession(infoCache, dialect, async session =>
        {
            await persister.Save(saga, session, saga.CorrelationProperty);
            await persister.Save(otherSaga, session, otherSaga.CorrelationProperty);
            await timeoutPersister.Request(saga, CreateStoredTimeout("a", DateTimeOffset.UtcNow), session, new ContextBag());
            await timeoutPersister.Request(saga, CreateStoredTimeout("b", DateTimeOffset.UtcNow), session, new ContextBag());
            await timeoutPersister.Request(otherSaga, CreateStoredTimeout("c", DateTimeOffset.UtcNow), session, new ContextBag());
        });

        // a failed concurrency check must not remove the timeouts
        await InSession(infoCache, dialect, async session =>
        {
            await Assert.ThatAsync(() => persister.Complete(saga, session, 99), Throws.TypeOf<Exception>());
            Assert.That(await timeoutPersister.Get(saga, session, new ContextBag()), Has.Count.EqualTo(2));
        }, commit: false);

        await InSession(infoCache, dialect, session => persister.Complete(saga, session, 1));

        await InSession(infoCache, dialect, async session =>
        {
            Assert.That(await timeoutPersister.Get(saga, session, new ContextBag()), Is.Empty);
            Assert.That(await timeoutPersister.Get(otherSaga, session, new ContextBag()), Has.Count.EqualTo(1));
        });
    }

#pragma warning disable PS0013, PS0018 // test helper, no cancellation needed
    async Task InSession(SagaInfoCache infoCache, SqlDialect dialect, Func<StorageSession, Task> action, bool commit = true)
    {
        using (var connection = dbConnection())
        using (var storageSession = new StorageSession(new FakeConnectionManager(connection), infoCache, dialect))
        {
            await storageSession.Open(null);
            await action(storageSession);
            if (commit)
            {
                await storageSession.CompleteAsync();
            }
        }
    }

#pragma warning restore PS0013, PS0018

    // Oracle's existing saga tables are shared between these sequential tests; keep the timeout prefix within its identifier limit too.
    string TimeoutEndpointName(string endpointName) => sqlDialect == BuildSqlDialect.Oracle ? "SagaTests" : endpointName;

    void CreateTimeoutSchema(string endpointName)
    {
        using (var connection = GetConnection()(schema))
        {
            connection.Open();
            connection.ExecuteCommand(SagaTimeoutScriptBuilder.BuildDropScript(sqlDialect), TimeoutEndpointName(endpointName), schema: schema);
            connection.ExecuteCommand(SagaTimeoutScriptBuilder.BuildCreateScript(sqlDialect), TimeoutEndpointName(endpointName), schema: schema);
        }
    }

    static StoredSagaTimeout CreateStoredTimeout(string value, DateTimeOffset dueTime) => new()
    {
        TimeoutId = Guid.NewGuid().ToString(),
        TriggerId = Guid.NewGuid().ToString(),
        Version = 1,
        DueTime = dueTime,
        MessageType = "MyNamespace.MyTimeout",
        ContentType = "application/json",
        Payload = System.Text.Encoding.UTF8.GetBytes($$"""{"Value":"{{value}}"}""")
    };

    static void AssertTimeoutsAreEqual(StoredSagaTimeout actual, StoredSagaTimeout expected)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual.TimeoutId, Is.EqualTo(expected.TimeoutId));
            Assert.That(actual.TriggerId, Is.EqualTo(expected.TriggerId));
            Assert.That(actual.Version, Is.EqualTo(expected.Version));
            Assert.That(actual.DueTime, Is.EqualTo(expected.DueTime));
            Assert.That(actual.DueTime.Offset, Is.EqualTo(expected.DueTime.Offset));
            Assert.That(actual.MessageType, Is.EqualTo(expected.MessageType));
            Assert.That(actual.ContentType, Is.EqualTo(expected.ContentType));
            Assert.That(actual.Payload, Is.EqualTo(expected.Payload));
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

            Assert.That(sagaData, Is.Not.Default);
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
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition3, sqlDialect), endpointName, schema: schema);
            Assert.Multiple(() =>
            {
                Assert.That(PropertyExists(TestTableName("TransitionalProcess", "CorrAndTransitionalSaga"), CorrelationPropertyName("Property1")), Is.False);
                Assert.That(PropertyExists(TestTableName("TransitionalProcess", "CorrAndTransitionalSaga"), CorrelationPropertyName("Property2")), Is.True);
            });
        }
    }

    [Test]
    public void CreateWithSameTableInAnotherSchema()
    {
        if (!SupportsSchemas())
        {
            Assert.Ignore();
        }

        var endpointName = nameof(CreateWithSameTableInAnotherSchema);
        var definitionWithTransitional = new SagaDefinition(
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
        var definition = new SagaDefinition(
            tableSuffix: "CorrAndTransitionalSaga",
            name: "CorrAndTransitionalSaga",
            correlationProperty: new CorrelationProperty
            (
                name: "Property1",
                type: CorrelationPropertyType.String
            )
        );

        // The same table in the default schema has a correlation column that the table in the custom schema does not
        DropAndCreate(definitionWithTransitional, endpointName, null);

        // Purging obsolete columns in the custom schema must not pick up the column from the default schema
        Assert.DoesNotThrow(() => DropAndCreate(definition, endpointName, schema));
    }

    [Test]
    public void ReplaceCorrelationAndTransitional()
    {
        var endpointName = nameof(ReplaceCorrelationAndTransitional);
        var table = TestTableName(endpointName, "CorrAndTransitionalSaga");
        using (var connection = dbConnection())
        {
            var definition1 = new SagaDefinition(
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
            connection.Open();
            connection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(definition1, sqlDialect), endpointName, schema: schema);
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition1, sqlDialect), endpointName, schema: schema);
            Assert.Multiple(() =>
            {
                Assert.That(PropertyExists(table, CorrelationPropertyName("Property1")), Is.True);
                Assert.That(PropertyExists(table, CorrelationPropertyName("Property2")), Is.True);
            });

            // Neither of the existing correlation columns is kept, so two columns (and their indexes) need to be purged
            var definition2 = new SagaDefinition(
                tableSuffix: "CorrAndTransitionalSaga",
                name: "CorrAndTransitionalSaga",
                correlationProperty: new CorrelationProperty
                (
                    name: "Property3",
                    type: CorrelationPropertyType.String
                )
            );
            connection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(definition2, sqlDialect), endpointName, schema: schema);
            Assert.Multiple(() =>
            {
                Assert.That(PropertyExists(table, CorrelationPropertyName("Property1")), Is.False);
                Assert.That(PropertyExists(table, CorrelationPropertyName("Property2")), Is.False);
                Assert.That(PropertyExists(table, CorrelationPropertyName("Property3")), Is.True);
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

            var exception = await Assert.ThrowsAsync<Exception>(() => persister.Update(sagaData.Data, storageSession, wrongVersion));
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

    [SqlSaga(transitionalCorrelationProperty: nameof(SagaData.TransitionalCorrelationProperty))]
    public class CorrAndTransitionalSaga :
        Saga<CorrAndTransitionalSaga.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
            public string TransitionalCorrelationProperty { get; set; }
            public string SimpleProperty { get; set; }
        }

        public Task Handle(AMessage message, IMessageHandlerContext context) => Task.CompletedTask;
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.CorrelationProperty).ToMessage<AMessage>(m => m.StringId);
    }

    public class SagaWithCorrelation :
        Saga<SagaWithCorrelation.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
            public string SimpleProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.CorrelationProperty).ToMessage<AMessage>(m => m.StringId);

        public Task Handle(AMessage message, IMessageHandlerContext context) => Task.CompletedTask;
    }

    [Test]
    [Explicit]
    public void TransitionId()
    {
        using var connection = dbConnection();
        TransitionIdInner(connection);
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
        Saga<SagaWithStringCorrelation.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.CorrelationProperty).ToMessage<AMessage>(m => m.StringId);

        public Task Handle(AMessage message, IMessageHandlerContext context) => Task.CompletedTask;
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
        Saga<NonStringCorrelationSaga.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public int CorrelationProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.CorrelationProperty).ToMessage<AMessage>(m => m.IntId);


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
            var throwsAsync = await Assert.ThrowsAsync<Exception>(async () =>
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
        Saga<SagaWithNoCorrelation.SagaData>,
        IAmStartedByMessages<AMessage>
    {
        public class SagaData : ContainSagaData
        {
            public string SimpleProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.ConfigureFinderMapping<AMessage, CustomFinder>();

        public Task Handle(AMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        public class CustomFinder : ISagaFinder<SagaData, AMessage>
        {
            public Task<SagaData> FindBy(AMessage message, ISynchronizedStorageSession session, IReadOnlyContextBag context, CancellationToken cancellationToken = default) => null;
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