using System.Data.Common;

namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the current <see cref="DbTransaction"/> and <see cref="DbConnection"/>.
    /// <seealso cref="SqlPersistenceStorageSessionExtensions.SqlPersistenceSession"/>
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
}