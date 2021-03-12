using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Outbox;

class TransactionScopeSqlOutboxTransaction : ISqlOutboxTransaction
{
    static ILog Log = LogManager.GetLogger<TransactionScopeSqlOutboxTransaction>();

    IConnectionManager connectionManager;
    IsolationLevel isolationLevel;
    ConcurrencyControlStrategy concurrencyControlStrategy;
    TransactionScope transactionScope;
    Transaction ambientTransaction;
    bool commit;

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
    {
        return concurrencyControlStrategy.Complete(outboxMessage, Connection, null, context);
    }

    public void BeginSynchronizedSession(ContextBag context)
    {
        if (System.Transactions.Transaction.Current != null && System.Transactions.Transaction.Current != ambientTransaction)
        {
            Log.Warn("The endpoint is configured to use Outbox with TransactionScope but a different TransactionScope " +
                     "has been detected in the current context. " +
                     "Do not use config.UnitOfWork().WrapHandlersInATransactionScope().");
        }
    }

    public void Dispose()
    {
        Connection?.Dispose();
        if (transactionScope != null)
        {
            if (commit)
            {
                transactionScope.Complete();
            }
            transactionScope.Dispose();
            transactionScope = null;
            ambientTransaction = null;
        }
    }

    public Task Commit(CancellationToken cancellationToken)
    {
        commit = true;
        return Task.FromResult(0);
    }
}