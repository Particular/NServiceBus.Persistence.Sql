namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Threading.Tasks;
    using System.Data.Common;
    using Extensibility;
    using ObjectBuilder;

    /// <summary>
    /// Exposes the current <see cref="DbTransaction"/> and <see cref="DbConnection"/>.
    /// In order to have these values passed via dependency injection, use <see cref="SqlStorageSession"/> class.
    /// </summary>
    public interface ISqlStorageSession
    {
        /// <summary>
        /// The current <see cref="DbTransaction"/>.
        /// </summary>
        DbTransaction Transaction { get; }

        /// <summary>
        /// The current <see cref="DbConnection"/>.
        /// </summary>
        DbConnection Connection { get; }

        /// <summary>
        /// Registers a callback to be called before completing the session.
        /// </summary>
        void OnSaveChanges(Func<ISqlStorageSession, Task> callback);
    }

    /// <summary>
    /// Exposes the current <see cref="DbTransaction"/> and <see cref="DbConnection"/> via a DI infrastructure.
    /// <seealso cref="SqlPersistenceStorageSessionExtensions.SqlPersistenceSession"/>
    /// </summary>
    public class SqlStorageSession
    {
        /// <summary>
        /// Creates a new instance of the storage session.
        /// </summary>
        public SqlStorageSession(DbConnection connection, DbTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        internal SqlStorageSession()
        {
        }

        /// <summary>
        /// The current <see cref="DbTransaction"/>.
        /// </summary>
        public DbTransaction Transaction { get; private set; }

        /// <summary>
        /// The current <see cref="DbConnection"/>.
        /// </summary>
        public DbConnection Connection { get; private set; }

        internal void SetConnectionAndTransaction(DbConnection connection, DbTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }
    }

    static class SqlStorageSessionExtensions
    {
        public static void RegisterSession(this ContextBag contextBag, ISqlStorageSession session)
        {
            var builder = contextBag.Get<IBuilder>();
            var unitOfWork = builder.Build<SqlStorageSession>();
            unitOfWork.SetConnectionAndTransaction(session.Connection, session.Transaction);
        }
    }

}