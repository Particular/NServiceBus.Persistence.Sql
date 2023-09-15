namespace NServiceBus.PersistenceTesting
{
    using Persistence.Sql.ScriptBuilder;

    class SqlTestVariant
    {
        public SqlTestVariant(SqlDialect dialect, BuildSqlDialect buildDialect, bool usePessimisticMode, bool supportsDtc)
        {
            Dialect = dialect;
            BuildDialect = buildDialect;
            UsePessimisticMode = usePessimisticMode;
            SupportsDtc = supportsDtc;
        }

        public SqlDialect Dialect { get; }

        public BuildSqlDialect BuildDialect { get; }

        public bool UsePessimisticMode { get; set; }

        public bool SupportsDtc { get; set; }

        public override string ToString() => $"{Dialect.GetType().Name}-pessimistic={UsePessimisticMode}";
    }
}