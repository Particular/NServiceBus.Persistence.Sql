using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

sealed class AdoNetSqlOutboxTransaction(
    ConcurrencyControlStrategy concurrencyControlStrategy,
    IConnectionManager connectionManager,
    IsolationLevel isolationLevel)
    : ISqlOutboxTransaction
{
    public DbTransaction Transaction { get; private set; }
    public DbConnection Connection { get; private set; }

    public void Prepare(ContextBag context)
    {
        //NOOP
    }

    public async Task Begin(ContextBag context, CancellationToken cancellationToken = default)
    {
        var incomingMessage = context.GetIncomingMessage();
        Connection = await connectionManager.OpenConnection(incomingMessage, cancellationToken).ConfigureAwait(false);
        Transaction = await Connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
        await concurrencyControlStrategy.Begin(incomingMessage.MessageId, Connection, Transaction, cancellationToken).ConfigureAwait(false);
    }

    public Task Complete(OutboxMessage outboxMessage, ContextBag context, CancellationToken cancellationToken = default) =>
        concurrencyControlStrategy.Complete(outboxMessage, Connection, Transaction, context, cancellationToken);

    public void Dispose()
    {
        Transaction?.Dispose();
        Connection?.Dispose();

        Transaction = null;
        Connection = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (Transaction is not null)
        {
            await Transaction.DisposeAsync().ConfigureAwait(false);
        }

        if (Connection is not null)
        {
            await Connection.DisposeAsync().ConfigureAwait(false);
        }

        Transaction = null;
        Connection = null;
    }

    public Task Commit(CancellationToken cancellationToken = default)
    {
        Transaction.Commit();
        return Task.CompletedTask;
    }
}