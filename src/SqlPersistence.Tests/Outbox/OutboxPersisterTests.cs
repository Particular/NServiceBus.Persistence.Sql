using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Outbox;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using ObjectApproval;

public abstract class OutboxPersisterTests
{
    OutboxPersister persister;
    BuildSqlVariant sqlVariant;
    Func<DbConnection> dbConnection;

    protected abstract Func<DbConnection> GetConnection();

    public OutboxPersisterTests(BuildSqlVariant sqlVariant, string schema)
    {
        this.sqlVariant = sqlVariant;
        dbConnection = GetConnection();
        persister = new OutboxPersister(connectionBuilder: dbConnection,
            tablePrefix: $"{nameof(OutboxPersisterTests)}_",
            schema: schema,
            sqlVariant: sqlVariant.Convert(),
            cleanupBatchCount: 5);
    }


    [SetUp]
    public void Setup()
    {
        using (var connection = dbConnection())
        {
            connection.Open();
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlVariant), nameof(OutboxPersisterTests));
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(sqlVariant), nameof(OutboxPersisterTests));
        }
    }

    [TearDown]
    public void TearDown()
    {
        using (var connection = dbConnection())
        {
            connection.Open();
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlVariant), nameof(OutboxPersisterTests));
        }
    }


    [Test]
    public void StoreDispatchAndGet()
    {
        var result = StoreDispatchAndGetAsync().GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result);
        VerifyOperationsAreEmpty(result);
    }

    void VerifyOperationsAreEmpty(OutboxMessage result)
    {
        using (var connection = dbConnection())
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"
select Operations
from {nameof(OutboxPersisterTests)}_OutboxData
where MessageId = '{result.MessageId}'";
                using (var reader = command.ExecuteReader())
                {
                    reader.Read();
                    var operations = reader.GetString(0);
                    Assert.AreEqual("[]", operations);
                }
            }
        }
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

        using (var connection = await dbConnection.OpenConnection())
        using (var transaction = connection.BeginTransaction())
        {
            await persister.Store(new OutboxMessage(messageId, operations.ToArray()), transaction, connection);
            transaction.Commit();
        }
        await persister.SetAsDispatched(messageId, null);
        return await persister.Get(messageId, null);
    }

    [Test]
    public void StoreAndGet()
    {
        var result = StoreAndGetAsync().GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result);
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
        using (var connection = await dbConnection.OpenConnection())
        using (var transaction = connection.BeginTransaction())
        {
            await persister.Store(new OutboxMessage(messageId, operations), transaction, connection);
            transaction.Commit();
        }
        return await persister.Get(messageId, null);
    }

    [Test]
    public async Task StoreAndCleanup()
    {
        using (var connection = await dbConnection.OpenConnection())
        {
            for (var i = 0; i < 13; i++)
            {
                await Store(i, connection);
            }
        }
        
        await Task.Delay(1000);
        var dateTime = DateTime.UtcNow;
        await Task.Delay(1000);
        using (var connection = await dbConnection.OpenConnection())
        {
            await Store(13, connection);
        }

            await persister.RemoveEntriesOlderThan(dateTime, CancellationToken.None);
        Assert.IsNull(await persister.Get("MessageId1", null));
        Assert.IsNull(await persister.Get("MessageId12", null));
        Assert.IsNotNull(await persister.Get("MessageId13", null));
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
            await persister.Store(outboxMessage, transaction, connection);
            transaction.Commit();
        }
        await persister.SetAsDispatched(messageId, null);
    }
}