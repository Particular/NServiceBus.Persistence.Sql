using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Transport;
using NUnit.Framework;
using IsolationLevel = System.Data.IsolationLevel;

[TestFixture]
public class StorageSessionConnectionLeakTests
{
    [Test]
    public async Task Open_disposes_connection_when_begin_transaction_throws()
    {
        var connection = new FakeConnection();
        var session = new StorageSession(new FakeConnectionManager(connection), null, new SqlDialect.MsSqlServer());

        Assert.ThrowsAsync<OperationCanceledException>(() => session.Open(null));
        await session.DisposeAsync();

        Assert.That(connection.WasDisposed, Is.True, "The connection was opened but never disposed.");
    }

    static readonly object[] dialects =
    [
        new object[] { new SqlDialect.MsSqlServer() },
        new object[] { new SqlDialect.Oracle() },
        new object[] { new SqlDialect.PostgreSql() }
    ];

    [TestCaseSource(nameof(dialects))]
    public async Task TryOpen_disposes_connection_when_enlist_throws(SqlDialect dialect)
    {
        var connection = new FakeConnection();
        var session = new StorageSession(new FakeConnectionManager(connection), null, dialect);

        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            Assert.ThrowsAsync<NotSupportedException>(async () => await session.TryOpen(new TransportTransaction(), null));
        }
        await session.DisposeAsync();

        Assert.That(connection.WasDisposed, Is.True, "The connection was opened but never disposed.");
    }

    class FakeConnectionManager(DbConnection connection) : IConnectionManager
    {
        public DbConnection BuildNonContextual() => connection;

        public DbConnection Build(IncomingMessage incomingMessage) => connection;
    }

    class FakeConnection : DbConnection
    {
        ConnectionState state = ConnectionState.Closed;

        public bool WasDisposed { get; private set; }

        public override string ConnectionString { get; set; } = "";
        public override string Database => "";
        public override string DataSource => "";
        public override string ServerVersion => "";
        public override ConnectionState State => state;

        public override void Open() => state = ConnectionState.Open;

        public override void Close() => state = ConnectionState.Closed;

        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

        // For example cancelled at shutdown
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new OperationCanceledException();

        // For example when distributed transactions are not supported
        public override void EnlistTransaction(Transaction transaction) => throw new NotSupportedException("Distributed transactions are not supported");

        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
