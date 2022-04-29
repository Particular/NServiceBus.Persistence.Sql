namespace NServiceBus.PersistenceTesting
{
    using Persistence.Sql.ScriptBuilder;

    class SqlTestVariant
    {
        public SqlTestVariant(SqlDialect dialect, BuildSqlDialect buildDialect, bool usePessimisticMode)
        {
            Dialect = dialect;
            BuildDialect = buildDialect;
            UsePessimisticMode = usePessimisticMode;
        }

        public SqlDialect Dialect { get; }
        public BuildSqlDialect BuildDialect { get; }
        public bool UsePessimisticMode { get; set; }

        public override string ToString() => $"{Dialect.GetType().Name}-pessimistic={UsePessimisticMode}";
    }
}