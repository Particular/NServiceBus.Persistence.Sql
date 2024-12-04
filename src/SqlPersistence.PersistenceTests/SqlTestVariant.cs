namespace NServiceBus.PersistenceTesting;

using System.Data;
using Persistence.Sql.ScriptBuilder;

class SqlTestVariant(SqlDialect dialect,
    BuildSqlDialect buildDialect,
    bool usePessimisticModeForSagas,
    bool usePessimisticModeForOutbox,
    bool supportsDtc,
    IsolationLevel isolationLevel,
    bool useTransactionScope,
    System.Transactions.IsolationLevel scopeIsolationLevel)
{
    public SqlDialect Dialect { get; } = dialect;

    public BuildSqlDialect BuildDialect { get; } = buildDialect;

    public bool UsePessimisticModeForSagas { get; } = usePessimisticModeForSagas;

    public bool UsePessimisticModeForOutbox { get; } = usePessimisticModeForOutbox;

    public bool SupportsDtc { get; } = supportsDtc;

    public IsolationLevel IsolationLevel { get; } = isolationLevel;

    public bool UseTransactionScope { get; } = useTransactionScope;

    public System.Transactions.IsolationLevel ScopeIsolationLevel { get; } = scopeIsolationLevel;

    public override string ToString()
    {
        var sagaMode = UsePessimisticModeForSagas ? "pessimistic" : "optimistic";
        var outboxMode = UsePessimisticModeForOutbox ? "pessimistic" : "optimistic";
        var transaction = UseTransactionScope ? $"TransactionScope({ScopeIsolationLevel})" : $"Ado({IsolationLevel})";
        return $"{Dialect.GetType().Name}-Sagas({sagaMode})-Outbox({outboxMode})-{transaction}";
    }
}