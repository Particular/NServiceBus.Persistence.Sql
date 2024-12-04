namespace NServiceBus.PersistenceTesting;

using System.Data;
using Persistence.Sql.ScriptBuilder;

class SqlTestVariant(SqlDialect dialect,
    BuildSqlDialect buildDialect,
    bool usePessimisticMode,
    bool supportsDtc,
    IsolationLevel isolationLevel,
    bool useTransactionScope,
    System.Transactions.IsolationLevel scopeIsolationLevel)
{
    public SqlDialect Dialect { get; } = dialect;

    public BuildSqlDialect BuildDialect { get; } = buildDialect;

    public bool UsePessimisticMode { get; } = usePessimisticMode;

    public bool SupportsDtc { get; } = supportsDtc;

    public IsolationLevel IsolationLevel { get; } = isolationLevel;

    public bool UseTransactionScope { get; } = useTransactionScope;

    public System.Transactions.IsolationLevel ScopeIsolationLevel { get; } = scopeIsolationLevel;

    public override string ToString()
    {
        var mode = UsePessimisticMode ? "pessimistic" : "optimistic";
        var transaction = UseTransactionScope ? $"transactionscope({ScopeIsolationLevel})" : $"ado({IsolationLevel})";
        return $"{Dialect.GetType().Name}-{mode}-{transaction}";
    }
}