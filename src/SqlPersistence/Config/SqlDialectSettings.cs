namespace NServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SqlDialectSettings<T> where T : SqlDialect
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class MySqlDialectSettings : SqlDialectSettings<SqlDialect.MySql>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class MsSqlDialectSettings : SqlDialectSettings<SqlDialect.MsSqlServer>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class OracleDialectSettings : SqlDialectSettings<SqlDialect.Oracle>
    {
    }
}