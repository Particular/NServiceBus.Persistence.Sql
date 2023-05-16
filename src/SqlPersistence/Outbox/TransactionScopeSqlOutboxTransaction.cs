using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

class TransactionScopeSqlOutboxTransaction : ISqlOutboxTransaction
{
    IConnectionManager connectionManager;
    IsolationLevel isolationLevel;
    ConcurrencyControlStrategy concurrencyControlStrategy;
    TransactionScope transactionScope;
    Transaction ambientTransaction;
    TimeSpan transactionTimeout;

    public TransactionScopeSqlOutboxTransaction(ConcurrencyControlStrategy concurrencyControlStrategy,
        IConnectionManager connectionManager, IsolationLevel isolationLevel, TimeSpan transactionTimeout)
    {
        this.connectionManager = connectionManager;
        this.isolationLevel = isolationLevel;
        this.concurrencyControlStrategy = concurrencyControlStrategy;
        this.transactionTimeout = transactionTimeout;
    }

    public DbTransaction Transaction => null;
    public DbConnection Connection { get; private set; }

    // Prepare is deliberately kept sync to allow floating of TxScope where needed
    public void Prepare(ContextBag context)
    {
        var options = new TransactionOptions
        {
            IsolationLevel = isolationLevel,
            Timeout = transactionTimeout // TimeSpan.Zero is default of `TransactionOptions.Timeout`
        };

        transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, options, TransactionScopeAsyncFlowOption.Enabled);
        ambientTransaction = System.Transactions.Transaction.Current;
    }

    public async Task Begin(ContextBag context, CancellationToken cancellationToken = default)
    {
        var incomingMessage = context.GetIncomingMessage();
        Connection = await connectionManager.OpenConnection(incomingMessage, cancellationToken).ConfigureAwait(false);
        Connection.EnlistTransaction(ambientTransaction);
        await concurrencyControlStrategy.Begin(incomingMessage.MessageId, Connection, null, cancellationToken).ConfigureAwait(false);
    }

    public Task Complete(OutboxMessage outboxMessage, ContextBag context, CancellationToken cancellationToken = default) =>
        concurrencyControlStrategy.Complete(outboxMessage, Connection, null, context, cancellationToken);

    public void Dispose()
    {
        Connection?.Dispose();
        transactionScope = null;
        ambientTransaction = null;
    }

    public Task Commit(CancellationToken cancellationToken = default)
    {
        transactionScope?.Complete();
        // we need to dispose it after completion in order to execute the transaction after marking it as completed
        transactionScope?.Dispose();
        return Task.CompletedTask;
    }
}