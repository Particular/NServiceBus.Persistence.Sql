namespace NServiceBus
{
    /// <summary>
    /// Exposes settings options available for the selected database engine.
    /// </summary>
    public abstract class SqlDialectSettings
    {
        internal SqlDialect Dialect;

        /// <summary>
        /// Exposes settings options available for the selected database engine.
        /// </summary>
        protected SqlDialectSettings(SqlDialect dialect)
        {
            Dialect = dialect;
        }
    }

    /// <summary>
    /// Exposes settings options available for the selected database engine.
    /// </summary>
    public class SqlDialectSettings<T> : SqlDialectSettings
        where T : SqlDialect, new()
    {
        /// <summary>
        /// Exposes settings options available for the selected database engine.
        /// </summary>
        public SqlDialectSettings() : base(new T())
        {
        }

        internal T TypedDialect => (T)Dialect;
    }
}