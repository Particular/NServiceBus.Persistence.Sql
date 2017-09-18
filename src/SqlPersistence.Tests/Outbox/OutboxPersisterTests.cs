using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Outbox;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
#if NET452
using ObjectApproval;
#endif

public abstract class OutboxPersisterTests
{
    OutboxPersister persister;
    BuildSqlDialect sqlDialect;
    string schema;
    Func<DbConnection> dbConnection;

    protected abstract Func<DbConnection> GetConnection();

    public OutboxPersisterTests(BuildSqlDialect sqlDialect, string schema)
    {
        this.sqlDialect = sqlDialect;
        this.schema = schema;
        dbConnection = GetConnection();
    }


    [SetUp]
    public void Setup()
    {
        persister = new OutboxPersister(
            connectionBuilder: dbConnection,
            tablePrefix: $"{GetTablePrefix()}_",
            sqlDialect: sqlDialect.Convert(schema),
            cleanupBatchSize: 5);
        using (var connection = dbConnection())
        {
            connection.Open();
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlDialect), GetTablePrefix(), schema: schema);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(sqlDialect), GetTablePrefix(), schema: schema);
        }
    }

    [TearDown]
    public void TearDown()
    {
        using (var connection = dbConnection())
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
        using (var connection = dbConnection())
        {
            connection.Open();
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(sqlDialect), GetTablePrefix(), schema: schema);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(sqlDialect), GetTablePrefix(), schema: schema);
        }
    }

    [Test]
    public void StoreDispatchAndGet()
    {
        var result = StoreDispatchAndGetAsync().GetAwaiter().GetResult();
#if NET452
        ObjectApprover.VerifyWithJson(result);
#endif
        VerifyOperationsAreEmpty(result);
    }

    void VerifyOperationsAreEmpty(OutboxMessage result)
    {
        using (var connection = dbConnection())
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = BuildOperationsFromMessageIdCommand(result.MessageId);
                using (var reader = command.ExecuteReader())
                {
                    reader.Read();
                    var operations = reader.GetString(0);
                    Assert.AreEqual("[]", operations);
                }
            }
        }
    }

    protected virtual string BuildOperationsFromMessageIdCommand(string messageId)
    {
        string tableName;
        if (string.IsNullOrEmpty(schema))
        {
            tableName = GetTablePrefix();
        }
        else
        {
            tableName = $"{schema}.{GetTablePrefix()}";
        }

        return $@"
select Operations
from {tableName}{GetTableSuffix()}
where MessageId = '{messageId}'";
    }

    async Task<OutboxMessage> StoreDispatchAndGetAsync()
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

        using (var connection = await dbConnection.OpenConnection().ConfigureAwait(false))
        using (var transaction = connection.BeginTransaction())
        {
            await persister.Store(new OutboxMessage(messageId, operations.ToArray()), transaction, connection).ConfigureAwait(false);
            transaction.Commit();
        }
        await persister.SetAsDispatched(messageId, null).ConfigureAwait(false);
        return await persister.Get(messageId, null).ConfigureAwait(false);
    }

    [Test]
    public void StoreAndGet()
    {
        var result = StoreAndGetAsync().GetAwaiter().GetResult();
        Assert.IsNotNull(result);
#if NET452
        ObjectApprover.VerifyWithJson(result);
#endif
    }

    async Task<OutboxMessage> StoreAndGetAsync()
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
        using (var connection = await dbConnection.OpenConnection().ConfigureAwait(false))
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
        using (var connection = await dbConnection.OpenConnection().ConfigureAwait(false))
        {
            for (var i = 0; i < 13; i++)
            {
                await Store(i, connection).ConfigureAwait(false);
            }
        }

        await Task.Delay(1000).ConfigureAwait(false);
        var dateTime = DateTime.UtcNow;
        await Task.Delay(1000).ConfigureAwait(false);
        using (var connection = await dbConnection.OpenConnection().ConfigureAwait(false))
        {
            await Store(13, connection).ConfigureAwait(false);
        }

        await persister.RemoveEntriesOlderThan(dateTime, CancellationToken.None).ConfigureAwait(false);
        Assert.IsNull(await persister.Get("MessageId1", null).ConfigureAwait(false));
        Assert.IsNull(await persister.Get("MessageId12", null).ConfigureAwait(false));
        Assert.IsNotNull(await persister.Get("MessageId13", null).ConfigureAwait(false));
    }

    async Task Store(int i, DbConnection connection)
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
}