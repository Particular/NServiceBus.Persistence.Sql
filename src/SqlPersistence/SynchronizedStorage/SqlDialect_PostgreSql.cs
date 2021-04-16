namespace NServiceBus
{
    using System;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using Transport;

    public partial class SqlDialect
    {
        public partial class PostgreSql
        {
            internal override async Task<StorageSession> TryAdaptTransportConnection(
                TransportTransaction transportTransaction,
                ContextBag context,
                IConnectionManager connectionBuilder,
                Func<DbConnection, DbTransaction, bool, StorageSession> storageSessionFactory,
                CancellationToken cancellationToken = default)
            {
                // Transport supports DTC and uses TxScope owned by the transport
                var scopeTx = Transaction.Current;

                if (transportTransaction.TryGet(out Transaction transportTx) &&
                    scopeTx != null &&
                    transportTx != scopeTx)
                {
                    throw new Exception("A TransactionScope has been opened in the current context overriding the one created by the transport. "
                                        + "This setup can result in inconsistent data because operations done via connections enlisted in the context scope won't be committed "
                                        + "atomically with the receive transaction. To manually control the TransactionScope in the pipeline switch the transport transaction mode "
                                        + $"to values lower than '{nameof(TransportTransactionMode.TransactionScope)}'.");
                }

                var ambientTransaction = transportTx ?? scopeTx;

                if (ambientTransaction == null)
                {
                    // Other modes handled by creating a new session.
                    return null;
                }

                var connection = await connectionBuilder.OpenConnection(context.GetIncomingMessage(), cancellationToken).ConfigureAwait(false);
                connection.EnlistTransaction(ambientTransaction);

                return storageSessionFactory(connection, null, true);
            }
        }
    }
}
