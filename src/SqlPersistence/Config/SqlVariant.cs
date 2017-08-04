namespace NServiceBus.Persistence.Sql
{
    using System;

    /// <summary>
    /// Allows for configuring which database engine to target. Used by <see cref="SqlPersistenceConfig.SqlVariant"/>.
    /// </summary>
    [Obsolete]
    public enum SqlVariant
    {
        /// <summary>
        /// Microsoft SQL Server.
        /// </summary>
        MsSqlServer,

        /// <summary>
        /// MySQL.
        /// </summary>
        MySql,

        /// <summary>
        /// Oracle.
        /// </summary>
        Oracle
    }
}