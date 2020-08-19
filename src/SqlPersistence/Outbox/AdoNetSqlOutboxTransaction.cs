using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Outbox;

class AdoNetSqlOutboxTransaction : ISqlOutboxTransaction
{
    static ILog Log = LogManager.GetLogger<AdoNetSqlOutboxTransaction>();

    IConnectionManager connectionManager;
    ConcurrencyControlStrategy concurrencyControlStrategy;

    public AdoNetSqlOutboxTransaction(ConcurrencyControlStrategy concurrencyControlStrategy, IConnectionManager connectionManager)
    {
        this.connectionManager = connectionManager;
        this.concurrencyControlStrategy = concurrencyControlStrategy;
    }

    public DbTransaction Transaction { get; private set; }
    public DbConnection Connection { get; private set; }

    public void Prepare(ContextBag context)
    {
        //NOOP
    }

    public async Task<OutboxTransaction> Begin(ContextBag context)
    {
        var incomingMessage = context.GetIncomingMessage();
        Connection = await connectionManager.OpenConnection(incomingMessage).ConfigureAwait(false);
        Transaction = Connection.BeginTransaction();
        await concurrencyControlStrategy.Begin(incomingMessage.MessageId, Connection, Transaction).ConfigureAwait(false);
        return this;
    }

    public Task Complete(OutboxMessage outboxMessage, ContextBag context)
    {
        return concurrencyControlStrategy.Complete(outboxMessage, Connection, Transaction, context);
    }

    public void BeginSynchronizedSession(ContextBag context)
    {
        if (System.Transactions.Transaction.Current != null)
        {
            Log.Warn("The endpoint is configured to use Outbox but a TransactionScope has been detected. " +
                     "In order to make the Outbox compatible with TransactionScope, use " +
                     "config.EnableOutbox().UseTransactionScope(). " +
                     "Do not use config.UnitOfWork().WrapHandlersInATransactionScope().");
        }
    }

    public void Dispose()
    {
        Transaction?.Dispose();
        Connection?.Dispose();
    }

    public Task Commit()
    {
        Transaction.Commit();
        return Task.FromResult(0);
    }
}