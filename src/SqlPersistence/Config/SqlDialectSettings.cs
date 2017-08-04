namespace NServiceBus
{
    /// <summary>
    /// Exposes settings options available for the selected database engine.
    /// </summary>
    public abstract class SqlDialectSettings<T> where T : SqlDialect
    {
    }

    /// <summary>
    /// Exposes settings options available for MySQL.
    /// </summary>
    public class MySqlDialectSettings : SqlDialectSettings<SqlDialect.MySql>
    {
    }

    /// <summary>
    /// Exposes settings options available for MS SQL Server.
    /// </summary>
    public class MsSqlDialectSettings : SqlDialectSettings<SqlDialect.MsSqlServer>
    {
    }

    /// <summary>
    /// Exposes settings options available for Oracle.
    /// </summary>
    public class OracleDialectSettings : SqlDialectSettings<SqlDialect.Oracle>
    {
    }
}