using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.Transport;
using NServiceBus.Unicast.Subscriptions;
using NUnit.Framework;

[TestFixture]
public class SubscriptionCacheFailureTests
{
    static readonly MessageType messageType = new("type1", new Version(0, 0, 0, 0));

    [Test]
    public void Failed_lookup_should_not_be_cached()
    {
        var connectionManager = new FailingConnectionManager(_ => throw new TimeoutException("transient"));
        var persister = CreatePersister(connectionManager);

        Assert.ThrowsAsync<TimeoutException>(() => persister.GetSubscribers(messageType));
        Assert.ThrowsAsync<TimeoutException>(() => persister.GetSubscribers(messageType));

        Assert.That(connectionManager.Attempts, Is.EqualTo(2), "The second lookup should query the database again instead of returning the cached failure.");
    }

    [Test]
    public void Cancelled_lookup_should_not_be_cached()
    {
        using var firstCaller = new CancellationTokenSource();
        var connectionManager = new FailingConnectionManager(attempt =>
        {
            if (attempt == 1)
            {
                firstCaller.Cancel();
                throw new OperationCanceledException(firstCaller.Token);
            }

            throw new TimeoutException("reached the database");
        });
        var persister = CreatePersister(connectionManager);

        Assert.CatchAsync<OperationCanceledException>(() => persister.GetSubscribers(messageType, firstCaller.Token));

        // A different caller, that has not cancelled, should not observe the first caller's cancellation
        Assert.ThrowsAsync<TimeoutException>(() => persister.GetSubscribers(messageType, CancellationToken.None));
        Assert.That(connectionManager.Attempts, Is.EqualTo(2));
    }

    [Test]
    public async Task Successful_refresh_should_survive_an_older_lookup_failure()
    {
        var firstLookup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connectionManager = new StubConnectionManager(attempt => attempt == 1 ? firstLookup.Task : Task.CompletedTask);
        var persister = CreatePersister(connectionManager);

        var pendingLookup = persister.GetSubscribers(messageType);

        // Expire the entry while the first connection is still opening, without waiting for the cache duration.
        persister.Cache.Values.Single().Stored = DateTime.UtcNow.AddMinutes(-2);
        var refreshedSubscribers = await persister.GetSubscribers(messageType);
        Assert.That(refreshedSubscribers.Select(subscriber => subscriber.TransportAddress), Is.EqualTo(new[] { "subscriber" }));

        firstLookup.SetException(new TimeoutException("older lookup failed"));
        Assert.ThrowsAsync<TimeoutException>(() => pendingLookup);

        var cachedSubscribers = await persister.GetSubscribers(messageType);

        Assert.Multiple(() =>
        {
            Assert.That(cachedSubscribers, Is.SameAs(refreshedSubscribers), "The successful refresh should remain cached after the older lookup fails.");
            Assert.That(connectionManager.Attempts, Is.EqualTo(2), "A later caller should use the successful refresh instead of opening another connection.");
        });
    }

    static SubscriptionPersister CreatePersister(IConnectionManager connectionManager) =>
        new(
            connectionManager: connectionManager,
            tablePrefix: "Prefix_",
            sqlDialect: new SqlDialect.MsSqlServer(),
            cacheFor: TimeSpan.FromMinutes(1));

    class FailingConnectionManager(Action<int> onAttempt) : IConnectionManager
    {
        int attempts;

        public int Attempts => attempts;

        public DbConnection BuildNonContextual()
        {
            onAttempt(Interlocked.Increment(ref attempts));
            throw new InvalidOperationException("onAttempt is expected to throw");
        }

        public DbConnection Build(IncomingMessage incomingMessage) => throw new NotSupportedException();
    }

    class StubConnectionManager(Func<int, Task> onOpen) : IConnectionManager
    {
        int attempts;

        public int Attempts => attempts;

        public DbConnection BuildNonContextual() => new StubConnection(onOpen(Interlocked.Increment(ref attempts)));

        public DbConnection Build(IncomingMessage incomingMessage) => throw new NotSupportedException();
    }

    class StubConnection(Task opening) : DbConnection
    {
        public override string ConnectionString { get; set; }
        public override string Database => "test";
        public override string DataSource => "test";
        public override string ServerVersion => "1";
        public override ConnectionState State => ConnectionState.Open;
        public override Task OpenAsync(CancellationToken cancellationToken = default) => opening;
        public override void Open() => throw new NotSupportedException();
        public override void Close() { }
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new StubCommand();
    }

    class StubCommand : DbCommand
    {
        readonly SqlCommand parameterContainer = new();

        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbTransaction DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection => parameterContainer.Parameters;
        protected override DbParameter CreateDbParameter() => new SqlParameter();
        public override void Cancel() { }
        public override void Prepare() { }
        public override int ExecuteNonQuery() => throw new NotSupportedException();
        public override object ExecuteScalar() => throw new NotSupportedException();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            var table = new DataTable();
            table.Columns.Add("Subscriber", typeof(string));
            table.Columns.Add("Endpoint", typeof(string));
            table.Rows.Add("subscriber", "endpoint");
            return table.CreateDataReader();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                parameterContainer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
