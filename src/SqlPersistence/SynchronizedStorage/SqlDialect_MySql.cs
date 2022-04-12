namespace NServiceBus
{
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    public partial class SqlDialect
    {
        public partial class MySql
        {
            // MySQL does not support DTC so we should not enlist if transport has such a transaction.
            internal override ValueTask<(bool WasAdapted, DbConnection Connection, DbTransaction Transaction, bool OwnsTransaction)> TryAdaptTransportConnection(
                TransportTransaction transportTransaction,
                ContextBag context,
                IConnectionManager connectionManager,
                CancellationToken cancellationToken = default) =>
                new ValueTask<(bool WasAdapted, DbConnection Connection, DbTransaction Transaction, bool OwnsTransaction)>((WasAdapted: false, Connection: null, Transaction: null, OwnsTransaction: false));
        }
    }
}
