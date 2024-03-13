namespace NServiceBus
{
    using System;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Persistence.Sql;
    using Sagas;

    /// <summary>
    /// Shared session extensions for SqlPersistence.
    /// </summary>
    public static class SqlPersistenceStorageSessionExtensions
    {
        /// <summary>
        /// Gets the current context SqlPersistence <see cref="ISqlStorageSession" />.
        /// </summary>
        public static ISqlStorageSession SqlPersistenceSession(this ISynchronizedStorageSession session)
        {
            ArgumentNullException.ThrowIfNull(session);

            if (session is ISqlStorageSession storageSession)
            {
                return storageSession;
            }

            throw new Exception("Cannot access the SQL synchronized storage session. Either this endpoint has not been configured to use the SQL persistence or a different persistence type is used for sagas.");
        }

        static StorageSession GetInternalSqlStorageSession(this ISynchronizedStorageSession session)
        {
            if (session is StorageSession storageSession)
            {
                return storageSession;
            }

            throw new Exception("Cannot access the SQL synchronized storage session. Either this endpoint has not been configured to use the SQL persistence or a different persistence type is used for sagas.");
        }

        /// <summary>
        /// Retrieves a <see cref="IContainSagaData" /> instance. Used when implementing a <see cref="ISagaFinder{TSagaData, TMessage}" />.
        /// </summary>
        /// <typeparam name="TSagaData">The <see cref="IContainSagaData" /> type to return.</typeparam>
        /// <param name="session">
        /// Used to provide an extension point and access the current <see cref="DbConnection" /> and
        /// <see cref="DbTransaction" />.
        /// </param>
        /// <param name="context">Used to append a concurrency value that can be verified when the SagaData is persisted.</param>
        /// <param name="whereClause">The SQL where clause to append to the retrieve saga SQL statement.</param>
        /// <param name="appendParameters">Used to append <see cref="DbParameter" />s used in the <paramref name="whereClause" />.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        public static Task<TSagaData> GetSagaData<TSagaData>(this ISynchronizedStorageSession session, IReadOnlyContextBag context, string whereClause, ParameterAppender appendParameters, CancellationToken cancellationToken = default)
            where TSagaData : class, IContainSagaData
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(appendParameters);
            ArgumentException.ThrowIfNullOrWhiteSpace(whereClause);

            var writableContextBag = (ContextBag)context;
            var sqlSession = session.GetInternalSqlStorageSession();

            if (sqlSession.InfoCache == null)
            {
                throw new Exception("Cannot load saga data because the Sagas feature is disabled in the endpoint.");
            }

            return SagaPersister.GetByWhereClause<TSagaData>(whereClause, session, writableContextBag, appendParameters, sqlSession.InfoCache, cancellationToken);
        }
    }
}