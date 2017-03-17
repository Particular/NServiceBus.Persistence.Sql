using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
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
            cleanupBatchSize: 5);
    }


    [SetUp]
    public void Setup()
    {
        using (var connection = dbConnection())
        {
            connection.Open();
            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlVariant), nameof(OutboxPersisterTests));
            ExecuteOutboxCreateCommand(connection, OutboxScriptBuilder.BuildCreateScript(sqlVariant), nameof(OutboxPersisterTests), 10);
        }
    }

    static void ExecuteOutboxCreateCommand(DbConnection connection, string script, string tablePrefix, int inboxRowCount)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = script;
            command.AddParameter("tablePrefix", $"{tablePrefix}_");
            if (connection is SqlConnection)
            {
                command.AddParameter("schema", "dbo");
            }
            command.AddParameter("inboxRowCount", inboxRowCount);
            command.ExecuteNonQuery();
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
        VerifyRowIsDeleted(result);
    }

    void VerifyRowIsDeleted(OutboxMessage result)
    {
        using (var connection = dbConnection())
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"
select count(*)
from {nameof(OutboxPersisterTests)}_OutboxData
where MessageId = '{result.MessageId}'";

                var count = command.ExecuteScalar();
                Assert.AreEqual(0, count);
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