using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Pipeline;
using NServiceBus.Transport.SQLServer;
using NUnit.Framework;

[TestFixture]
public class UserDataConsistencyTests
{
    static ManualResetEvent manualResetEvent = new ManualResetEvent(false);
    string endpointName = "SqlTransportIntegration";

    string createUserDataTableText = @"
IF NOT  EXISTS (
    select * from sys.objects
    where object_id = object_id('[dbo].[SqlTransportIntegration_UserDataConsistencyTests_Data]')
    and type in ('U')
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
        using (var connection = MsSqlConnectionBuilder.Build())
        {
            connection.Open();
            SqlQueueDeletion.DeleteQueuesForEndpoint(connection, "dbo", endpointName);
        }
    }

    [Test]
    public Task In_DTC_mode_enlists_in_the_ambient_transaction()
    {
        Requires.DtcSupport();
        return RunTest(e =>
        {
            var transport = e.UseTransport<SqlServerTransport>();
            transport.UseCustomSqlConnectionFactory(async () =>
            {
                var connection = MsSqlConnectionBuilder.Build();
                await connection.OpenAsync().ConfigureAwait(false);
                return connection;
            });
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
            transport.UseCustomSqlConnectionFactory(async () =>
            {
                var connection = MsSqlConnectionBuilder.Build();
                await connection.OpenAsync().ConfigureAwait(false);
                return connection;
            });
        });
    }

    [Test]
    public Task In_outbox_mode_enlists_in_outbox_transaction()
    {
        return RunTest(configuration =>
        {
            configuration.GetSettings().Set("DisableOutboxTransportCheck", true);
            var transport = configuration.UseTransport<SqlServerTransport>();
            transport.UseCustomSqlConnectionFactory(async () =>
            {
                var connection = MsSqlConnectionBuilder.Build();
                await connection.OpenAsync().ConfigureAwait(false);
                return connection;
            });
            configuration.EnableOutbox();
        });
    }


    void Execute(string endpointName, string script)
    {
        using (var sqlConnection = MsSqlConnectionBuilder.Build())
        {
            sqlConnection.Open();
            using (var command = sqlConnection.CreateCommand())
            {
                command.CommandText = script;
                command.AddParameter("tablePrefix", $"{endpointName}_");
                command.AddParameter("schema", "dbo");
                command.ExecuteNonQuery();
            }
        }
    }
    void Execute(string script)
    {
        using (var sqlConnection = MsSqlConnectionBuilder.Build())
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

        Execute(endpointName, OutboxScriptBuilder.BuildDropScript(BuildSqlDialect.MsSqlServer));
        Execute(endpointName, OutboxScriptBuilder.BuildCreateScript(BuildSqlDialect.MsSqlServer));
        Execute(createUserDataTableText);

        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(endpointName);
        //Hack: enable outbox to force sql session since we have no saga
        endpointConfiguration.EnableOutbox();
        var typesToScan = TypeScanner.NestedTypes<UserDataConsistencyTests>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        endpointConfiguration.DisableFeature<TimeoutManager>();
        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        testCase(endpointConfiguration);
        transport.UseCustomSqlConnectionFactory(async () =>
        {
            var connection = MsSqlConnectionBuilder.Build();
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        });
        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        persistence.ConnectionBuilder(MsSqlConnectionBuilder.Build);
        persistence.DisableInstaller();
        persistence.SubscriptionSettings().DisableCache();
        endpointConfiguration.DefineCriticalErrorAction(c =>
        {
            message = c.Error;
            manualResetEvent.Set();
            return Task.FromResult(0);
        });
        endpointConfiguration.LimitMessageProcessingConcurrencyTo(1);
        endpointConfiguration.Pipeline.Register(new FailureTrigger(), "Failure trigger");

        var endpoint = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
        var dataId = Guid.NewGuid();

        var failingMessage = new FailingMessage
        {
            EntityId = dataId
        };
        await endpoint.SendLocal(failingMessage).ConfigureAwait(false);

        var checkMessage = new CheckMessage
        {
            EntityId = dataId
        };
        await endpoint.SendLocal(checkMessage).ConfigureAwait(false);

        manualResetEvent.WaitOne();
        await endpoint.Stop().ConfigureAwait(false);

        Assert.AreEqual("Success", message);
    }

    class FailureTrigger : Behavior<IIncomingLogicalMessageContext>
    {
        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);
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
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
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
                count = (int)await command.ExecuteScalarAsync().ConfigureAwait(false);
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