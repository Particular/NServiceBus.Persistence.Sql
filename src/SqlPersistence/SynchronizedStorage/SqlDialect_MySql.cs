namespace NServiceBus
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    public partial class SqlDialect
    {
        public partial class MySql
        {
            static Task<StorageSession> result = Task.FromResult((StorageSession)null);

            internal override Task<StorageSession> TryAdaptTransportConnection(TransportTransaction transportTransaction, ContextBag context, IConnectionManager connectionManager, Func<DbConnection, DbTransaction, bool, StorageSession> storageSessionFactory)
            {
                // MySQL does not support DTC so we should not enlist if transport has such a transaction.
                return result;
            }
        }
    }
}