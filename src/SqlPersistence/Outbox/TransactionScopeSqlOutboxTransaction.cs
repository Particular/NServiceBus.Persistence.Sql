using System.Data.Common;
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

    public TransactionScopeSqlOutboxTransaction(ConcurrencyControlStrategy concurrencyControlStrategy,
        IConnectionManager connectionManager, IsolationLevel isolationLevel)
    {
        this.connectionManager = connectionManager;
        this.isolationLevel = isolationLevel;
        this.concurrencyControlStrategy = concurrencyControlStrategy;
    }

    public DbTransaction Transaction => null;
    public DbConnection Connection { get; private set; }

    // Prepare is deliberately kept sync to allow floating of TxScope where needed
    public void Prepare(ContextBag context)
    {
        var options = new TransactionOptions
        {
            IsolationLevel = isolationLevel
        };

        transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, options, TransactionScopeAsyncFlowOption.Enabled);
        ambientTransaction = System.Transactions.Transaction.Current;
    }

    public async Task Begin(ContextBag context)
    {
        var incomingMessage = context.GetIncomingMessage();
        Connection = await connectionManager.OpenConnection(incomingMessage).ConfigureAwait(false);
        Connection.EnlistTransaction(ambientTransaction);
        await concurrencyControlStrategy.Begin(incomingMessage.MessageId, Connection, null).ConfigureAwait(false);
    }

    public Task Complete(OutboxMessage outboxMessage, ContextBag context)
        => concurrencyControlStrategy.Complete(outboxMessage, Connection, null, context);

    public void Dispose()
    {
        transactionScope?.Dispose();
        Connection?.Dispose();
        transactionScope = null;
        ambientTransaction = null;
    }

    public Task Commit()
    {
        transactionScope?.Complete();
        // we need to dispose it after completion in order to execute the transaction after marking it as completed
        transactionScope?.Dispose();
        return Task.CompletedTask;
    }
}