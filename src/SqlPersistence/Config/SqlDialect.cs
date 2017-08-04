namespace NServiceBus
{
    /// <summary>
    /// Allows for configuring which database engine to target. Used by <see cref="SqlPersistenceConfig.SqlDialect{T}"/>.
    /// </summary>
    public class SqlDialect
    {
        /// <summary>
        /// Microsoft SQL Server
        /// </summary>
        public class MsSqlServer : SqlDialect
        {
        }

        /// <summary>
        /// MySQL
        /// </summary>
        public class MySql : SqlDialect
        {
        }

        /// <summary>
        /// Oracle
        /// </summary>
        public class Oracle : SqlDialect
        {
        }
    }
}