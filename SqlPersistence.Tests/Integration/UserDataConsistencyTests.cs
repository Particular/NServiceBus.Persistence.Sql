using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
public class UserDataConsistencyTests
{
    static string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencetests;Integrated Security=True";
    static ManualResetEvent manualResetEvent = new ManualResetEvent(false);
    string endpointName = "SqlTransportIntegration";

    string createUserDataTableText = @"
IF NOT  EXISTS (
    select * from sys.objects
    where object_id = object_id(N'[dbo].[SqlTransportIntegration_UserDataConsistencyTests_Data]')
    and type in (N'U')
)
begin
    create table [dbo].[SqlTransportIntegration_UserDataConsistencyTests_Data](
        [Id] [uniqueidentifier] not null
    ) ON [PRIMARY];
end";

    [SetUp]
    [TearDown]
    public void Setup()
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            SqlQueueDeletion.DeleteQueuesForEndpoint(connection, "dbo", endpointName);
        }
    }

    [Test]
    public Task In_DTC_mode_enlists_in_the_ambient_transaction()
    {
        return RunTest(e =>
        {
            var transport = e.UseTransport<MsmqTransport>();
            transport.Transactions(TransportTransactionMode.TransactionScope);
        });
    }

    [Test]
    public Task In_native_SqlTransport_mode_enlists_in_native_transaction()
    {
        return RunTest(e =>
        {
            var transport = e.UseTransport<SqlServerTransport>();
            transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
            transport.ConnectionString(connectionString);
        });
    }

    [Test]
    public Task In_outbox_mode_enlists_in_outbox_transaction()
    {
        return RunTest(e =>
        {
            e.GetSettings().Set("DisableOutboxTransportCheck", true);
            e.UseTransport<MsmqTransport>();
            e.EnableOutbox();
        });
    }


    void Execute(string endpointName, string script)
    {
        using (var sqlConnection = new SqlConnection(connectionString))
        {
            sqlConnection.Open();
            using (var command = sqlConnection.CreateCommand())
            {
                command.CommandText = script;
                command.AddParameter("tablePrefix", $"{endpointName}_");
                command.ExecuteNonQuery();
            }
        }
    }
    void Execute(string script)
    {
        using (var sqlConnection = new SqlConnection(connectionString))
        {
            sqlConnection.Open();
            using (var command = sqlConnection.CreateCommand())
            {
                command.CommandText = script;
                command.ExecuteNonQuery();
            }
        }
    }
    async Task RunTest(Action<EndpointConfiguration> testCase)
    {
        manualResetEvent.Reset();
        string message = null;

        Execute(endpointName, OutboxScriptBuilder.BuildDropScript(BuildSqlVarient.MsSqlServer));
        Execute(endpointName, OutboxScriptBuilder.BuildCreateScript(BuildSqlVarient.MsSqlServer));
        Execute(createUserDataTableText);

        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(endpointName);
        var typesToScan = TypeScanner.NestedTypes<UserDataConsistencyTests>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        testCase(endpointConfiguration);
        transport.ConnectionString(connectionString);
        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(() => new SqlConnection(connectionString));
        persistence.DisableInstaller();
        endpointConfiguration.DefineCriticalErrorAction(c =>
        {
            message = c.Error;
            manualResetEvent.Set();
            return Task.CompletedTask;
        });
        endpointConfiguration.LimitMessageProcessingConcurrencyTo(1);
        endpointConfiguration.Pipeline.Register(new FailureTrigger(), "Failure trigger");

        var endpoint = await Endpoint.Start(endpointConfiguration);
        var dataId = Guid.NewGuid();
        await endpoint.SendLocal(new FailingMessage
        {
            EntityId = dataId
        });
        await endpoint.SendLocal(new CheckMessage
        {
            EntityId = dataId
        });
        manualResetEvent.WaitOne();
        await endpoint.Stop();

        Assert.AreEqual("Success", message);
    }

    class FailureTrigger : Behavior<IIncomingLogicalMessageContext>
    {
        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            await next();
            if (context.Message.Instance is FailingMessage)
            {
                throw new Exception("Boom!");
            }
        }
    }

    public class FailingMessage : IMessage
    {
        public Guid EntityId { get; set; }
    }

    public class CheckMessage : IMessage
    {
        public Guid EntityId { get; set; }
    }

    public class Handler :
        IHandleMessages<FailingMessage>,
        IHandleMessages<CheckMessage>
    {
        public CriticalError CriticalError { get; set; }

        public async Task Handle(FailingMessage message, IMessageHandlerContext context)
        {
            var session = context.SynchronizedStorageSession.SqlPersistenceSession();
            var commandText = "insert into [dbo].[SqlTransportIntegration_UserDataConsistencyTests_Data] (Id) VALUES (@Id)";
            using (var command = session.Connection.CreateCommand())
            {
                command.Transaction = session.Transaction;
                command.CommandText = commandText;
                command.AddParameter("@Id", message.EntityId);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task Handle(CheckMessage message, IMessageHandlerContext context)
        {
            int count;
            var session = context.SynchronizedStorageSession.SqlPersistenceSession();
            var commandText = "SELECT COUNT(*) from [dbo].[SqlTransportIntegration_UserDataConsistencyTests_Data] where Id = @Id";
            using (var command = session.Connection.CreateCommand())
            {
                command.Transaction = session.Transaction;
                command.CommandText = commandText;
                command.AddParameter("@Id", message.EntityId);
                count = (int)await command.ExecuteScalarAsync();
            }

            if (count > 0)
            {
                CriticalError.Raise("Failure", new Exception());
            }
            else
            {
                CriticalError.Raise("Success", new Exception());
            }
        }
    }
}