namespace NServiceBus
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Persistence.Sql;
    using Transport;

    public partial class SqlDialect
    {
        internal abstract Task<CompletableSynchronizedStorageSession> TryAdaptTransportConnection(TransportTransaction transportTransaction, ContextBag context, ConnectionManager connectionManager, Func<DbConnection, DbTransaction, bool, StorageSession> storageSessionFactory);
    }
}