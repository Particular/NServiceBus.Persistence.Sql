#pragma warning disable 1591

namespace NServiceBus.Persistence.Sql
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "3.0",
        RemoveInVersion = "4.0",
        ReplacementTypeOrMember = "persistence.SqlDialect<SqlDialect.DialectType>()")]
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

#pragma warning restore 1591