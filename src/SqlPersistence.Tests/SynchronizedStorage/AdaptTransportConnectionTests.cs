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
    protected IConnectionManager connectionManager;

    protected abstract Func<string, DbConnection> GetConnection();

    protected virtual bool SupportsDistributedTransactions => true;

    protected AdaptTransportConnectionTests(BuildSqlDialect sqlDialect)
    {
        this.sqlDialect = sqlDialect;
        connectionManager = new ConnectionManager(() => GetConnection()(null));
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
                var ex = Assert.ThrowsAsync<Exception>(async () => await sqlDialect.Convert().TryAdaptTransportConnection(transportTransaction, new ContextBag(), connectionManager));

                Assert.That(ex.Message, Does.StartWith("A TransactionScope has been opened in the current context overriding the one created by the transport."));
            }
        }
    }

    [Test]
    public async Task It_returns_false_if_there_is_no_transaction_scope()
    {
        var transportTransaction = new TransportTransaction();
        (bool wasAdapted, DbConnection _, DbTransaction _, bool _) = await sqlDialect.Convert().TryAdaptTransportConnection(transportTransaction, new ContextBag(), connectionManager);

        Assert.That(wasAdapted, Is.False);
    }
}