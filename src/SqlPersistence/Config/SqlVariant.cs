namespace NServiceBus.Persistence.Sql
{
    using System;

    /// <summary>
    /// Obsolete: Use 'persistence.SqlDialect&lt;SqlDialect.MsSqlServer&gt;()' (or other dialect) instead. Will be removed in version 4.0.0.
    /// </summary>
    [Obsolete("Use 'persistence.SqlDialect<SqlDialect.MsSqlServer>()' (or other dialect) instead. Will be removed in version 4.0.0.", true)]
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