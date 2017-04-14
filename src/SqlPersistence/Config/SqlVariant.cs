namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// Allows for configuring which database engine to target. Used by <see cref="SqlPersistenceConfig.SqlVariant"/>.
    /// </summary>
    public enum SqlVariant
    {
        /// <summary>
        /// Microsoft SQL Server.
        /// </summary>
        MsSqlServer,

        /// <summary>
        /// MySQL.
        /// </summary>
        MySql
    }
}