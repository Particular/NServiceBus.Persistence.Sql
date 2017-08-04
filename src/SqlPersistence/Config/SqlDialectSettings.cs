namespace NServiceBus
{
    /// <summary>
    /// Exposes settings options available for the selected database engine.
    /// </summary>
    public class SqlDialectSettings<T> where T : SqlDialect
    {
    }

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Shows how settings will be exposed for each SQL dialect.
        /// </summary>
        public static void Schema(this SqlDialectSettings<SqlDialect.MsSqlServer> dialectSettings)
        {
            
        }
    }
}