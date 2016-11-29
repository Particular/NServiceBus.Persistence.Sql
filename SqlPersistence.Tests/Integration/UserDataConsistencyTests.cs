using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
public class UserDataConsistencyTests
{
    static string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    static ManualResetEvent manualResetEvent = new ManualResetEvent(false);
    string endpointName = "SqlTransportIntegration";

    string createUserDataTableText = @"
IF NOT  EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[SqlTransportIntegration.UserDataConsistencyTests.Data]')
    AND type in (N'U')
)
BEGIN
    CREATE TABLE [dbo].[SqlTransportIntegration.UserDataConsistencyTests.Data](
        [Id] [uniqueidentifier] NOT NULL
    ) ON [PRIMARY];
END";

    [SetUp]
    [TearDown]
    public async Task Setup()
    {
        using (var connection = await SqlHelpers.New(connectionString))
        {
            await SqlQueueDeletion.DeleteQueuesForEndpoint(connection, "dbo", endpointName);
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

    async Task RunTest(Action<EndpointConfiguration> testCase)
    {
        manualResetEvent.Reset();
        string message = null;
        await DbBuilder.ReCreate(connectionString, endpointName);

        await SqlHelpers.Execute(connectionString, createUserDataTableText, collection => {});

        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(endpointName);
        var typesToScan = TypeScanner.NestedTypes<UserDataConsistencyTests>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        testCase(endpointConfiguration);
        transport.ConnectionString(connectionString);
        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.ConnectionString(connectionString);
        persistence.DisableInstaller();
        endpointConfiguration.DefineCriticalErrorAction(c =>
        {
            message = c.Error;
            manualResetEvent.Set();
            return Task.FromResult(0);
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
            var commandText = "INSERT INTO [dbo].[SqlTransportIntegration.UserDataConsistencyTests.Data] (Id) VALUES (@Id)";
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
            var commandText = "SELECT COUNT(*) FROM [dbo].[SqlTransportIntegration.UserDataConsistencyTests.Data] WHERE Id = @Id";
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