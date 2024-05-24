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
            internal override async ValueTask<(bool WasAdapted, DbConnection Connection, DbTransaction Transaction, bool OwnsTransaction)> TryAdaptTransportConnection(
                TransportTransaction transportTransaction,
                ContextBag context,
                IConnectionManager connectionBuilder,
                CancellationToken cancellationToken = default)
            {
                // SQL server transport in native TX mode
                if (transportTransaction.TryGet("System.Data.SqlClient.SqlConnection", out DbConnection existingSqlConnection) &&
                    transportTransaction.TryGet("System.Data.SqlClient.SqlTransaction", out DbTransaction existingSqlTransaction))
                {
                    if (existingSqlConnection.GetType().Name != "NpgsqlConnection")
                    {
                        return (WasAdapted: false, Connection: null, Transaction: null, OwnsTransaction: false);
                    }

                    return (WasAdapted: true, Connection: existingSqlConnection, Transaction: existingSqlTransaction, OwnsTransaction: false);
                }

                if (transportTransaction.TryGet(out Transaction _))
                {
                    throw new Exception("PostgreSQL persistence should not be used with TransportTransactionMode equal to TransactionScope");
                }

                // Transport supports DTC and uses TxScope owned by the transport
                var scopeTx = Transaction.Current;

                if (scopeTx == null)
                {
                    // Other modes handled by creating a new session.
                    return (WasAdapted: false, Connection: null, Transaction: null, OwnsTransaction: false);
                }

                var connection = await connectionBuilder.OpenConnection(context.GetIncomingMessage(), cancellationToken).ConfigureAwait(false);
                connection.EnlistTransaction(scopeTx);

                return (WasAdapted: true, Connection: connection, Transaction: null, OwnsTransaction: true);
            }
        }
    }
}
