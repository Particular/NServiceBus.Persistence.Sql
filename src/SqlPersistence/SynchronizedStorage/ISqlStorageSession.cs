using System.Data.Common;

namespace NServiceBus.Persistence.Sql
{
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
    }
}