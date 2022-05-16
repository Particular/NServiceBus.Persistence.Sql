namespace NServiceBus
{
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    public partial class SqlDialect
    {
        internal abstract ValueTask<(bool WasAdapted, DbConnection Connection, DbTransaction Transaction, bool OwnsTransaction)> TryAdaptTransportConnection(
            TransportTransaction transportTransaction,
            ContextBag context,
            IConnectionManager connectionManager,
            CancellationToken cancellationToken = default);
    }
}
