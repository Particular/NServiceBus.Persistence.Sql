namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Data.Common;
    using Persistence.Sql.ScriptBuilder;

    class SqlTestVariant
    {
        public SqlTestVariant(SqlDialect dialect, BuildSqlDialect buildDialect, Func<DbConnection> connectionFactory)
        {
            Dialect = dialect;
            BuildDialect = buildDialect;
            ConnectionFactory = connectionFactory;
        }

        public SqlDialect Dialect { get; }
        public BuildSqlDialect BuildDialect { get; }
        public  Func<DbConnection> ConnectionFactory { get; }

        public override string ToString()
        {
            return Dialect.GetType().Name;
        }
    }
}