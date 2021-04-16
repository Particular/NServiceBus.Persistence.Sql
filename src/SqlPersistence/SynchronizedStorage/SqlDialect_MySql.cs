namespace NServiceBus
{
    using System;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    public partial class SqlDialect
    {
        public partial class MySql
        {
            static readonly Task<StorageSession> result = Task.FromResult<StorageSession>(null);

            // MySQL does not support DTC so we should not enlist if transport has such a transaction.
            internal override Task<StorageSession> TryAdaptTransportConnection(
                TransportTransaction transportTransaction,
                ContextBag context,
                IConnectionManager connectionManager,
                Func<DbConnection, DbTransaction, bool, StorageSession> storageSessionFactory,
                CancellationToken cancellationToken = default) =>
                result;
        }
    }
}
