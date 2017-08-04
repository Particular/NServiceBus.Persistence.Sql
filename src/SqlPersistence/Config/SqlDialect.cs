namespace NServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public class SqlDialect
    {
        /// <summary>
        /// Microsoft SQL Server.
        /// </summary>
        public class MsSqlServer : SqlDialect
        {
        }

        /// <summary>
        /// MySQL.
        /// </summary>
        public class MySql : SqlDialect
        {
        }

        /// <summary>
        /// Oracle.
        /// </summary>
        public class Oracle : SqlDialect
        {
        }
    }
}