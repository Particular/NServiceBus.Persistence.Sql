using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Outbox;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using Particular.Approvals;

public abstract class OutboxPersisterTests
{
    BuildSqlDialect sqlDialect;
    string schema;
    IConnectionManager connectionManager;

    protected abstract Func<string, DbConnection> GetConnection();
    protected virtual bool SupportsSchemas() => true;

    public OutboxPersisterTests(BuildSqlDialect sqlDialect, string schema)
    {
        this.sqlDialect = sqlDialect;
        this.schema = schema;
        connectionManager = new ConnectionManager(() => GetConnection()(schema));
    }


    OutboxPersister Setup(string theSchema)
    {
        var persister = new OutboxPersister(
            connectionManager: connectionManager,
            tablePrefix: $"{GetTablePrefix()}_",
            sqlDialect: sqlDialect.Convert(theSchema),
            cleanupBatchSize: 5);
        using (var connection = GetConnection()(theSchema))
        {
            connection.Open();
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlDialect), GetTablePrefix(), schema: theSchema);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(sqlDialect), GetTablePrefix(), schema: theSchema);
        }
        return persister;
    }

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
        Approver.Verify(result);

        Assert.AreEqual(1, result.Item1.TransportOperations.Length);
        Assert.AreEqual(0, result.Item2.TransportOperations.Length);
    }

    async Task<Tuple<OutboxMessage, OutboxMessage>> StoreDispatchAndGetAsync(OutboxPersister persister)
    {
        var operations = new List<TransportOperation>
        {
            new TransportOperation(
                messageId: "Id1",
                options: new Dictionary<string, string>
                {
                    {
                        "OptionKey1", "OptionValue1"
                    }
                },
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

        using (var connection = await connectionManager.OpenNonContextualConnection().ConfigureAwait(false))
        using (var transaction = connection.BeginTransaction())
        {
            await persister.Store(new OutboxMessage(messageId, operations.ToArray()), transaction, connection).ConfigureAwait(false);
            transaction.Commit();
        }
        var beforeDispatch = await persister.Get(messageId, null).ConfigureAwait(false);
        await persister.SetAsDispatched(messageId, null).ConfigureAwait(false);
        var afterDispatch = await persister.Get(messageId, null).ConfigureAwait(false);

        return Tuple.Create(beforeDispatch, afterDispatch);
    }

    [Test]
    public void StoreAndGet()
    {
        var persister = Setup(schema);
        var result = StoreAndGetAsync(persister).GetAwaiter().GetResult();
        Assert.IsNotNull(result);
        Approver.Verify(result);
    }

    async Task<OutboxMessage> StoreAndGetAsync(OutboxPersister persister)
    {
        var operations = new[]
        {
            new TransportOperation(
                messageId: "Id1",
                options: new Dictionary<string, string>
                {
                    {
                        "OptionKey1", "OptionValue1"
                    }
                },
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
        using (var connection = await connectionManager.OpenNonContextualConnection().ConfigureAwait(false))
        using (var transaction = connection.BeginTransaction())
        {
            await persister.Store(new OutboxMessage(messageId, operations), transaction, connection).ConfigureAwait(false);
            transaction.Commit();
        }
        return await persister.Get(messageId, null).ConfigureAwait(false);
    }

    [Test]
    public async Task StoreAndCleanup()
    {
        var persister = Setup(schema);
        using (var connection = await connectionManager.OpenNonContextualConnection().ConfigureAwait(false))
        {
            for (var i = 0; i < 13; i++)
            {
                await Store(i, connection, persister).ConfigureAwait(false);
            }
        }

        await Task.Delay(1000).ConfigureAwait(false);
        var dateTime = DateTime.UtcNow;
        await Task.Delay(1000).ConfigureAwait(false);
        using (var connection = await connectionManager.OpenNonContextualConnection().ConfigureAwait(false))
        {
            await Store(13, connection, persister).ConfigureAwait(false);
        }

        await persister.RemoveEntriesOlderThan(dateTime, CancellationToken.None).ConfigureAwait(false);
        Assert.IsNull(await persister.Get("MessageId1", null).ConfigureAwait(false));
        Assert.IsNull(await persister.Get("MessageId12", null).ConfigureAwait(false));
        Assert.IsNotNull(await persister.Get("MessageId13", null).ConfigureAwait(false));
    }

    async Task Store(int i, DbConnection connection, OutboxPersister persister)
    {
        var operations = new[]
        {
            new TransportOperation(
                messageId: "OperationId" + i,
                options: new Dictionary<string, string>
                {
                    {
                        "OptionKey1", "OptionValue1"
                    }
                },
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
        var outboxMessage = new OutboxMessage(messageId, operations);

        using (var transaction = connection.BeginTransaction())
        {
            await persister.Store(outboxMessage, transaction, connection).ConfigureAwait(false);
            transaction.Commit();
        }
        await persister.SetAsDispatched(messageId, null).ConfigureAwait(false);
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
                options: new Dictionary<string, string>
                {
                    {
                        "OptionKey1", "OptionValue1"
                    }
                },
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

        using (var connection = GetConnection()(null))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            using (var transaction = connection.BeginTransaction())
            {
                await defaultSchemaPersister.Store(new OutboxMessage(messageId, operations.ToArray()), transaction, connection).ConfigureAwait(false);
                transaction.Commit();
            }
        }
        var result = await schemaPersister.Get(messageId, null).ConfigureAwait(false);
        Assert.IsNull(result);
    }
}