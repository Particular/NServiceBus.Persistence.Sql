using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NServiceBus.Outbox;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class OutboxPersisterTests
{
    string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    OutboxPersister persister;
    SqlVarient sqlVarient = SqlVarient.MsSqlServer;

    public OutboxPersisterTests()
    {
        persister = new OutboxPersister(
            connectionBuilder: () =>
            {
                DbConnection connection = new SqlConnection(connectionString);
                connection.Open();
                return connection.ToTask();
            },
            schema: "dbo",
            endpointName: "Endpoint",
            jsonSerializer: JsonSerializer.CreateDefault(),
            readerCreator: reader => new JsonTextReader(reader),
            writerCreator: builder =>
            {
                var writer = new StringWriter(builder);
                return new JsonTextWriter(writer);
            });
    }

    [SetUp]
    public void Setup()
    {
        Execute(OutboxScriptBuilder.BuildDropScript(sqlVarient));
        Execute(OutboxScriptBuilder.BuildCreateScript(sqlVarient));
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
        using (var connection = await SqlHelpers.New(connectionString))
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
        using (var connection = await SqlHelpers.New(connectionString))
        using (var transaction = connection.BeginTransaction())
        {
            await persister.Store(new OutboxMessage(messageId, operations), transaction, connection);
            transaction.Commit();
        }
        return await persister.Get(messageId, null);
    }
    
    void Execute(string script)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = script;
                command.AddParameter("schema", "dbo");
                command.AddParameter("endpointName", "Endpoint");
                command.ExecuteNonQuery();
            }
        }
    }
}