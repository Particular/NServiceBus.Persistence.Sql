namespace NServiceBus
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Transport;

    public partial class SqlDialect
    {
        public partial class PostgreSql
        {
            static Task<CompletableSynchronizedStorageSession> result = Task.FromResult((CompletableSynchronizedStorageSession)null);

            internal override Task<CompletableSynchronizedStorageSession> TryAdaptTransportConnection(TransportTransaction transportTransaction, ContextBag context, Func<DbConnection> connectionBuilder, Func<DbConnection, DbTransaction, bool, StorageSession> storageSessionFactory)
            {
                // PostgreSql does not support DTC so we should not enlist if transport has such a transaction.
                return result;
            }
        }
    }
}