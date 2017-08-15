namespace NServiceBus
{
    /// <summary>
    /// Allows for configuring which database engine to target. Used by <see cref="SqlPersistenceConfig.SqlDialect{T}"/>.
    /// </summary>
    public class SqlDialect
    {
        /// <summary>
        /// Allows for configuring which database engine to target. Used by <see cref="SqlPersistenceConfig.SqlDialect{T}"/>.
        /// </summary>
        protected SqlDialect()
        {    
        }

        /// <summary>
        /// Microsoft SQL Server
        /// </summary>
        public class MsSqlServer : SqlDialect
        {
            /// <summary>
            /// Microsoft SQL Server
            /// </summary>
            public MsSqlServer()
            {
                Schema = "dbo";
            }
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

        internal string Schema { get; set; }

        internal string Name => this.GetType().Name;

        /// <summary>
        /// Gets the name of the SqlDialect.
        /// </summary>
        public override string ToString()
        {
            return this.Name;
        }
    }
}