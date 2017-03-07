using System;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;

namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// Shared session extensions for SqlPersistence.
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

        static StorageSession GetSqlStorageSession(this SynchronizedStorageSession session)
        {
            var storageSession = session as StorageSession;
            if (storageSession != null)
            {
                return storageSession;
            }
            throw new InvalidOperationException("The endpoint has not been configured to use SQL persistence.");
        }

        public static Task<TSagaData> GetSagaData<TSagaData>(this SynchronizedStorageSession session, ReadOnlyContextBag readOnlyContextBag, string whereClause, ParameterAppender appendParameters)
            where TSagaData : IContainSagaData
        {
            var contextBag = (ContextBag)readOnlyContextBag;
            var sqlSession = session.GetSqlStorageSession();
            var sagaInfoCache = sqlSession.InfoCache;
            if (sagaInfoCache == null)
            {
                throw new Exception($"{nameof(GetSagaData)} can only be executed when Sagas have been enabled on the endpoint.");
            }
            return SagaPersister.GetByWhereClause<TSagaData>(whereClause, session, contextBag, appendParameters, sagaInfoCache);
        }
    }
}
