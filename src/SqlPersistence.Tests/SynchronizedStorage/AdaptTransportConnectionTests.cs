using System;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Transport;
using NUnit.Framework;

abstract class AdaptTransportConnectionTests
{
    protected BuildSqlDialect sqlDialect;
    protected ConnectionManager connectionManager;

    protected abstract Func<string, DbConnection> GetConnection();

    protected virtual bool SupportsDistributedTransactions => true;

    protected AdaptTransportConnectionTests(BuildSqlDialect sqlDialect)
    {
        this.sqlDialect = sqlDialect;
        connectionManager = new SingleTenantConnectionManager(() => GetConnection()(null));
    }

    [Test]
    public void It_throws_if_non_transport_transaction_scope_exists()
    {
        if (!SupportsDistributedTransactions)
        {
            Assert.Ignore();
        }
        using (new TransactionScope())
        {
            var transportTransaction = new TransportTransaction();
            transportTransaction.Set(Transaction.Current);
            using (new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                var ex = Assert.ThrowsAsync<Exception>(() => sqlDialect.Convert().TryAdaptTransportConnection(transportTransaction, new ContextBag(), connectionManager,
                    (conn, tx, arg3) => new StorageSession(conn, tx, false, null)));

                StringAssert.StartsWith("A TransactionScope has been opened in the current context overriding the one created by the transport.", ex.Message);
            }
        }
    }

    [Test]
    public async Task It_returns_null_if_there_is_no_transaction_scope()
    {
        var transportTransaction = new TransportTransaction();
        var result = await sqlDialect.Convert().TryAdaptTransportConnection(transportTransaction, new ContextBag(), connectionManager,
            (conn, tx, arg3) => new StorageSession(conn, tx, false, null));

        Assert.IsNull(result);
    }
}