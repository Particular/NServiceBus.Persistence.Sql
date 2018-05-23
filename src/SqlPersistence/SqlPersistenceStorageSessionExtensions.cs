namespace NServiceBus
{
    using System;
    using Persistence;
    using Persistence.Sql;
    using System.Data.Common;
    using System.Threading.Tasks;
    using Extensibility;
    using Sagas;

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
            Guard.AgainstNull(nameof(session), session);
            return GetSqlStorageSession(session);
        }

        static StorageSession GetSqlStorageSession(this SynchronizedStorageSession session)
        {
            if (session is StorageSession storageSession)
            {
                return storageSession;
            }
            throw new Exception("Cannot access the SQL synchronized storage session. Either this endpoint has not been configured to use the SQL persistence or a different persistence type is used for sagas.");
        }

        /// <summary>
        /// Retrieves a <see cref="IContainSagaData"/> instance. Used when implementing a <see cref="IFindSagas{T}"/>.
        /// </summary>
        /// <typeparam name="TSagaData">The <see cref="IContainSagaData"/> type to return.</typeparam>
        /// <param name="session">Used to provide an extension point and access the current <see cref="DbConnection"/> and <see cref="DbTransaction"/>.</param>
        /// <param name="context">Used to append a concurrency value that can be verified when the SagaData is persisted.</param>
        /// <param name="whereClause">The SQL where clause to append to the retrieve saga SQL statement.</param>
        /// <param name="appendParameters">Used to append <see cref="DbParameter"/>s used in the <paramref name="whereClause"/>.</param>
        public static Task<TSagaData> GetSagaData<TSagaData>(this SynchronizedStorageSession session, ReadOnlyContextBag context, string whereClause, ParameterAppender appendParameters)
            where TSagaData : class, IContainSagaData
        {
            Guard.AgainstNull(nameof(session), session);
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(appendParameters), appendParameters);
            Guard.AgainstNullAndEmpty(nameof(whereClause), whereClause);

            var writableContextBag = (ContextBag)context;
            var sqlSession = session.GetSqlStorageSession();

            if (sqlSession.InfoCache == null)
            {
                throw new Exception("Cannot load saga data because the Sagas feature is disabled in the endpoint.");
            }
            return SagaPersister.GetByWhereClause<TSagaData>(whereClause, session, writableContextBag, appendParameters, sqlSession.InfoCache);
        }
    }
}