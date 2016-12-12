using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using NServiceBus.Outbox;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class OutboxPersisterTests : IDisposable
{
    string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencetests;Integrated Security=True";
    OutboxPersister persister;
    SqlVarient sqlVarient = SqlVarient.MsSqlServer;
    DbConnection dbConnection;

    public OutboxPersisterTests()
    {
        dbConnection = new SqlConnection(connectionString);
        dbConnection.Open();
        persister = new OutboxPersister(
            connectionBuilder: () =>
            {
                DbConnection connection = new SqlConnection(connectionString);
                connection.Open();
                return connection.ToTask();
            },
            schema: "dbo",
            tablePrefix: $"{nameof(OutboxPersisterTests)}.");
    }


    [SetUp]
    public void Setup()
    {
        dbConnection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlVarient), nameof(OutboxPersisterTests));
        dbConnection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(sqlVarient), nameof(OutboxPersisterTests));
    }

    [TearDown]
    public void TearDown()
    {
        dbConnection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(sqlVarient), nameof(OutboxPersisterTests));
    }


    [Test]
    public void StoreDispatchAndGet()
    {
        var result = StoreDispatchAndGetAsync().GetAwaiter().GetResult();
        ObjectApprover.VerifyWithJson(result);
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
        using (var transaction = dbConnection.BeginTransaction())
        {
            await persister.Store(new OutboxMessage(messageId, operations.ToArray()), transaction, dbConnection);
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
        using (var transaction = dbConnection.BeginTransaction())
        {
            await persister.Store(new OutboxMessage(messageId, operations), transaction, dbConnection);
            transaction.Commit();
        }
        return await persister.Get(messageId, null);
    }

    public void Dispose()
    {
        dbConnection.Dispose();
    }
}