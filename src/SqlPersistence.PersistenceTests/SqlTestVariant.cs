namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Data.Common;
    using Persistence.Sql.ScriptBuilder;

    class SqlTestVariant
    {
        public SqlTestVariant(SqlDialect dialect, BuildSqlDialect buildDialect, Func<DbConnection> connectionFactory, bool usePessimisticMode)
        {
            Dialect = dialect;
            BuildDialect = buildDialect;
            ConnectionFactory = connectionFactory;
            UsePessimisticMode = usePessimisticMode;
        }

        public SqlDialect Dialect { get; }
        public BuildSqlDialect BuildDialect { get; }
        public Func<DbConnection> ConnectionFactory { get; }
        public bool UsePessimisticMode { get; set; }

        public override string ToString()
        {
            return $"{Dialect.GetType().Name}-pessimistic={UsePessimisticMode}";
        }
    }
}