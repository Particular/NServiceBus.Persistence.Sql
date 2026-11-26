using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Transport;
using NUnit.Framework;
using Particular.Approvals;
using TransportOperation = NServiceBus.Outbox.TransportOperation;

public abstract class OutboxPersisterTests
{
    BuildSqlDialect sqlDialect;
    string schema;
    bool pessimistic;
    bool transactionScope;

    protected abstract Func<string, DbConnection> GetConnection();
    protected virtual bool SupportsSchemas() => true;

    public OutboxPersisterTests(BuildSqlDialect sqlDialect, string schema, bool pessimistic, bool transactionScope)
    {
        this.sqlDialect = sqlDialect;
        this.schema = schema;
        this.pessimistic = pessimistic;
        this.transactionScope = transactionScope;
    }


    OutboxPersister Setup(string theSchema) => Build(theSchema).Persister;

    PersisterSetup Build(string theSchema)
    {
        var dialect = sqlDialect.Convert(theSchema);
        var outboxCommands = OutboxCommandBuilder.Build(dialect, $"{GetTablePrefix()}_");

        var connectionManager = new ConnectionManager(() => GetConnection()(theSchema));
        var persister = new OutboxPersister(
            connectionManager: connectionManager,
            sqlDialect: dialect,
            outboxCommands: outboxCommands,
            outboxTransactionFactory: () =>
            {
                ConcurrencyControlStrategy behavior;
                if (pessimistic)
                {
                    behavior = new PessimisticConcurrencyControlStrategy(dialect, outboxCommands);
                }
                else
                {
                    behavior = new OptimisticConcurrencyControlStrategy(dialect, outboxCommands);
                }

                return transactionScope
                    ? new TransactionScopeSqlOutboxTransaction(behavior, connectionManager, IsolationLevel.ReadCommitted, TimeSpan.Zero)
                    : new AdoNetSqlOutboxTransaction(behavior, connectionManager, System.Data.IsolationLevel.ReadCommitted);
            },
            cleanupBatchSize: 5);
        using (var connection = GetConnection()(theSchema))
        {
            connection.Open();
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlDialect), GetTablePrefix(), schema: theSchema);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(sqlDialect), GetTablePrefix(), schema: theSchema);
        }
        return new PersisterSetup(persister, dialect, outboxCommands);
    }

    sealed record PersisterSetup(OutboxPersister Persister, SqlDialect Dialect, OutboxCommands Commands);

    [TearDown]
    public void TearDown()
    {
        using (var connection = GetConnection()(null))
        {
            connection.Open();
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlDialect), GetTablePrefix(), schema: null);
        }
        using (var connection = GetConnection()(schema))
        {
            connection.Open();
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlDialect), GetTablePrefix(), schema: schema);
        }
    }

    protected virtual string GetTablePrefix()
    {
        return nameof(OutboxPersisterTests);
    }

    protected virtual string GetTableSuffix()
    {
        return "_OutboxData";
    }

    [Test]
    public void ExecuteCreateTwice()
    {
        var connectionManager = new ConnectionManager(() => GetConnection()(schema));

        using (var connection = connectionManager.BuildNonContextual())
        {
            connection.Open();
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(sqlDialect), GetTablePrefix(), schema: schema);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(sqlDialect), GetTablePrefix(), schema: schema);
        }
    }

    [Test]
    public void StoreDispatchAndGet()
    {
        var persister = Setup(schema);
        var result = StoreDispatchAndGetAsync(persister).GetAwaiter().GetResult();
        Approver.Verify(Serializer.Serialize(result));

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1.TransportOperations, Has.Length.EqualTo(1));
            Assert.That(result.Item2.TransportOperations, Is.Empty);
        });
    }

    static async Task<Tuple<OutboxMessage, OutboxMessage>> StoreDispatchAndGetAsync(OutboxPersister persister, CancellationToken cancellationToken = default)
    {
        var operations = new List<TransportOperation>
        {
            new TransportOperation(
                messageId: "Id1",
                properties: new DispatchProperties(new Dictionary<string, string>
                {
                    {
                        "OptionKey1", "OptionValue1"
                    }
                }),
                body: new byte[] {0x20, 0x21},
                headers: new Dictionary<string, string>
                {
                    {
                        "HeaderKey1", "HeaderValue1"
                    }
                }
            )
        };
        var messageId = "a";

        var contextBag = CreateContextBag(messageId);
        using (var transaction = await persister.BeginTransaction(contextBag, cancellationToken))
        {
            await persister.Store(new OutboxMessage(messageId, operations.ToArray()), transaction, contextBag, cancellationToken);
            await transaction.Commit(cancellationToken);
        }

        var beforeDispatch = await persister.Get(messageId, contextBag, cancellationToken);
        await persister.SetAsDispatched(messageId, contextBag, cancellationToken);
        var afterDispatch = await persister.Get(messageId, contextBag, cancellationToken);

        return Tuple.Create(beforeDispatch, afterDispatch);
    }

    [Test]
    public void StoreAndGet()
    {
        var persister = Setup(schema);
        var result = StoreAndGetAsync(persister).GetAwaiter().GetResult();
        Assert.That(result, Is.Not.Null);
        Approver.Verify(Serializer.Serialize(result));
    }

    [Test]
    public async Task StoreAndGetPreservesSpecialCharactersNullsAndEmptyBodies()
    {
        var persister = Setup(schema);
        var messageId = "a";
        var contextBag = CreateContextBag(messageId);
        using (var transaction = await persister.BeginTransaction(contextBag))
        {
            await persister.Store(new OutboxMessage(messageId, SpecialOperations()), transaction, contextBag);
            await transaction.Commit();
        }
        var result = await persister.Get(messageId, contextBag);

        AssertSpecialOperations(result.TransportOperations);
    }

    [Test]
    public async Task StoreAndGetPreservesLatin1Characters()
    {
        var persister = Setup(schema);
        var messageId = "a";
        var contextBag = CreateContextBag(messageId);
        var headers = new Dictionary<string, string>
        {
            ["NonBreakingSpace"] = "a\u00a0b",
            ["Umlaut"] = "ä",
            ["Accent"] = "é",
            ["Sharp"] = "ß",
            ["City"] = "Zürich"
        };
        using (var transaction = await persister.BeginTransaction(contextBag))
        {
            await persister.Store(new OutboxMessage(messageId, [new TransportOperation("Id1", [], new byte[] { 1 }, headers)]), transaction, contextBag);
            await transaction.Commit();
        }

        var result = await persister.Get(messageId, contextBag);

        Assert.That(result.TransportOperations, Has.Length.EqualTo(1));
        Assert.That(result.TransportOperations[0].Headers, Is.EqualTo(headers));
    }

    [Test]
    public async Task StoreAndGetLargeRecordWithSurrogatePairsAcrossChunks()
    {
        var persister = Setup(schema);
        var messageId = "a";
        var contextBag = CreateContextBag(messageId);
        var body = new byte[1024 * 1024];
        new Random(42).NextBytes(body);
        var emoji = string.Concat(Enumerable.Repeat("\U0001F600", 3000));
        // The second run is shifted by one char so a chunk boundary splits a surrogate pair whatever the parity.
        var headers = new Dictionary<string, string>
        {
            ["Emoji"] = emoji,
            ["ShiftedEmoji"] = "a" + emoji
        };
        using (var transaction = await persister.BeginTransaction(contextBag))
        {
            await persister.Store(new OutboxMessage(messageId, [new TransportOperation("Id1", [], body, headers)]), transaction, contextBag);
            await transaction.Commit();
        }

        var result = await persister.Get(messageId, contextBag);

        Assert.That(result.TransportOperations, Has.Length.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TransportOperations[0].Body.ToArray(), Is.EqualTo(body));
            Assert.That(result.TransportOperations[0].Headers, Is.EqualTo(headers));
        }
    }

    [Test]
    public async Task GetReadsRecordWrittenByNewtonsoft()
    {
        var setup = Build(schema);
        var messageId = "a";
        var json = Serializer.Serialize(SpecialOperations().ToSerializable());
        json = setup.Dialect.AddOutboxPadding(json);

        using (var connection = GetConnection()(schema))
        {
            connection.Open();
            using var command = setup.Dialect.CreateCommand(connection);
            command.CommandText = setup.Commands.OptimisticStore;
            command.AddParameter("MessageId", messageId);
            command.AddJsonParameter("Operations", json);
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            await command.ExecuteNonQueryEx();
        }

        var result = await setup.Persister.Get(messageId, CreateContextBag(messageId));

        AssertSpecialOperations(result.TransportOperations);
    }

    [Test]
    public async Task NewtonsoftReadsRecordWrittenBySystemTextJson()
    {
        var setup = Build(schema);
        var messageId = "a";
        var contextBag = CreateContextBag(messageId);
        using (var transaction = await setup.Persister.BeginTransaction(contextBag))
        {
            await setup.Persister.Store(new OutboxMessage(messageId, SpecialOperations()), transaction, contextBag);
            await transaction.Commit();
        }

        SerializableOperation[] stored;
        using (var connection = GetConnection()(schema))
        {
            connection.Open();
            using var command = setup.Dialect.CreateCommand(connection);
            command.CommandText = setup.Commands.Get;
            command.AddParameter("MessageId", messageId);
            using var dataReader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess | System.Data.CommandBehavior.SingleRow);
            Assert.That(await dataReader.ReadAsync(), Is.True);
            _ = await dataReader.GetBoolAsync(0);
            using var textReader = dataReader.GetTextReader(1);
            stored = Serializer.Deserialize<IEnumerable<SerializableOperation>>(textReader).ToArray();
        }

        AssertSpecialOperations(stored.FromSerializable());
    }

    static Dictionary<string, string> SpecialHeaders() => new()
    {
        ["Quote"] = "\"quoted\"",
        ["Backslash"] = "a\\b",
        ["NewLine"] = "a\nb",
        ["Plus"] = "a+b",
        ["Backtick"] = "a`b",
        ["Html"] = "<a href='x'>&</a>",
        ["Unicode"] = "Zürich 😀",
        ["Null"] = null,
        ["Empty"] = ""
    };

    static TransportOperation[] SpecialOperations() =>
    [
        new TransportOperation("Id1", new DispatchProperties(new Dictionary<string, string> { ["Option"] = "a\"b" }), new byte[0], SpecialHeaders()),
        new TransportOperation("Id2", [], new byte[] { 1, 2, 3 }, new Dictionary<string, string> { ["Key"] = "Value" })
    ];

    static void AssertSpecialOperations(TransportOperation[] operations)
    {
        Assert.That(operations, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(operations[0].MessageId, Is.EqualTo("Id1"));
            Assert.That(operations[0].Body.ToArray(), Is.Empty);
            Assert.That(operations[0].Headers, Is.EqualTo(SpecialHeaders()));
            Assert.That(operations[0].Options, Is.EqualTo(new Dictionary<string, string> { ["Option"] = "a\"b" }));
            Assert.That(operations[1].MessageId, Is.EqualTo("Id2"));
            Assert.That(operations[1].Body.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(operations[1].Headers, Is.EqualTo(new Dictionary<string, string> { ["Key"] = "Value" }));
            Assert.That(operations[1].Options, Is.Empty);
        });
    }

    [Test]
    public async Task TransactionScope()
    {
        if (!transactionScope)
        {
            Assert.Ignore();
        }
        var persister = Setup(schema);

        var messageId = "a";
        var contextBag = CreateContextBag(messageId);
        using (var transaction = await persister.BeginTransaction(contextBag))
        {
            var ambientTransaction = Transaction.Current;
            Assert.That(ambientTransaction, Is.Not.Null);

            await transaction.Commit();
        }
    }

    static async Task<OutboxMessage> StoreAndGetAsync(OutboxPersister persister, CancellationToken cancellationToken = default)
    {
        var operations = new[]
        {
            new TransportOperation(
                messageId: "Id1",
                properties: new DispatchProperties(new Dictionary<string, string>
                {
                    {
                        "OptionKey1", "OptionValue1"
                    }
                }),
                body: new byte[] {0x20, 0x21},
                headers: new Dictionary<string, string>
                {
                    {
                        "HeaderKey1", "HeaderValue1"
                    }
                }
            )
        };

        var messageId = "a";

        var contextBag = CreateContextBag(messageId);
        using (var transaction = await persister.BeginTransaction(contextBag, cancellationToken))
        {
            await persister.Store(new OutboxMessage(messageId, operations), transaction, contextBag, cancellationToken);
            await transaction.Commit(cancellationToken);
        }
        return await persister.Get(messageId, contextBag, cancellationToken);
    }

    static ContextBag CreateContextBag(string messageId)
    {
        var contextBag = new ContextBag();
        contextBag.Set(new IncomingMessage(messageId, [], new byte[0]));
        return contextBag;
    }

    [Test]
    public async Task StoreAndCleanup()
    {
        var persister = Setup(schema);
        for (var i = 0; i < 13; i++)
        {
            await Store(i, persister);
        }

        await Task.Delay(1000);
        var dateTime = DateTime.UtcNow;
        await Task.Delay(1000);
        await Store(13, persister);

        await persister.RemoveEntriesOlderThan(dateTime);
        Assert.Multiple(async () =>
        {
            Assert.That(await persister.Get("MessageId1", null).ConfigureAwait(false), Is.Null);
            Assert.That(await persister.Get("MessageId12", null).ConfigureAwait(false), Is.Null);
            Assert.That(await persister.Get("MessageId13", null).ConfigureAwait(false), Is.Not.Null);
        });
    }

    static async Task Store(int i, OutboxPersister persister, CancellationToken cancellationToken = default)
    {
        var operations = new[]
        {
            new TransportOperation(
                messageId: "OperationId" + i,
                properties: new DispatchProperties(new Dictionary<string, string>
                {
                    {
                        "OptionKey1", "OptionValue1"
                    }
                }),
                body: new byte[]
                {
                    0x20
                },
                headers: new Dictionary<string, string>
                {
                    {
                        "HeaderKey1", "HeaderValue1"
                    }
                }
            )
        };
        var messageId = "MessageId" + i;
        var contextBag = CreateContextBag(messageId);
        using (var transaction = await persister.BeginTransaction(contextBag, cancellationToken))
        {
            await persister.Store(new OutboxMessage(messageId, operations), transaction, contextBag, cancellationToken);
            await transaction.Commit(cancellationToken);
        }
        await persister.SetAsDispatched(messageId, contextBag, cancellationToken);
    }

    [Test]
    public async Task UseConfiguredSchema()
    {
        if (!SupportsSchemas())
        {
            Assert.Ignore();
        }

        var schemaPersister = Setup(schema);
        var defaultSchemaPersister = Setup(null);

        var operations = new List<TransportOperation>
        {
            new TransportOperation(
                messageId: "Id1",
                properties: new DispatchProperties(new Dictionary<string, string>
                {
                    {
                        "OptionKey1", "OptionValue1"
                    }
                }),
                body: new byte[] {0x20, 0x21},
                headers: new Dictionary<string, string>
                {
                    {
                        "HeaderKey1", "HeaderValue1"
                    }
                }
            )
        };
        var messageId = "a";
        var contextBag = CreateContextBag(messageId);
        using (var transaction = await defaultSchemaPersister.BeginTransaction(contextBag))
        {
            await defaultSchemaPersister.Store(new OutboxMessage(messageId, operations.ToArray()), transaction, contextBag);
            await transaction.Commit();
        }

        var result = await schemaPersister.Get(messageId, contextBag);
        Assert.That(result, Is.Null);
    }
}