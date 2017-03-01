using System;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;

namespace NServiceBus
{
    using System.Data.Common;
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// Shared session extensions for NHibernate persistence.
    /// </summary>
    public static class SqlPersistenceStorageSessionExtensions
    {
        /// <summary>
        /// Gets the current context SqlPersistence <see cref="ISqlStorageSession"/>.
        /// </summary>
        public static ISqlStorageSession SqlPersistenceSession(this SynchronizedStorageSession session)
        {
            return GetSqlStorageSession(session);
        }

        internal static StorageSession GetSqlStorageSession(this SynchronizedStorageSession session)
        {
            var storageSession = session as StorageSession;
            if (storageSession == null)
            {
                throw new InvalidOperationException("Shared session has not been configured for SqlPersistence.");
            }
            return storageSession;
        }

        public static Task<TSagaData> GetSagaData<TSagaData>(this SynchronizedStorageSession session, ReadOnlyContextBag readOnlyContextBag, string whereClause, Action<DbCommand> addParameters)
            where TSagaData : IContainSagaData
        {
            var contextBag = (ContextBag)readOnlyContextBag;
            var sqlSession = session.GetSqlStorageSession();
            var sagaInfoCache = sqlSession.InfoCache;
            var sagaType = contextBag.GetSagaType();
            var runtimeSagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData), sagaType);
            var query = $@"
{runtimeSagaInfo.SelectFromCommand}
where {whereClause}";
            return SagaPersister.GetSagaData<TSagaData>(runtimeSagaInfo, contextBag, sqlSession,
                command =>
                {
                    command.CommandText = query;
                    addParameters(command);
                });
        }
    }
}