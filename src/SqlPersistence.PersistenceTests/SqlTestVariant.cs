namespace NServiceBus.PersistenceTesting;

using System.Data;

class SqlTestVariant(DatabaseEngine databaseEngine,
    IsolationLevel isolationLevel,
    bool useTransactionScope,
    System.Transactions.IsolationLevel scopeIsolationLevel,
    bool usePessimisticModeForOutbox)
{
    public DatabaseEngine DatabaseEngine { get; } = databaseEngine;

    public bool UsePessimisticModeForOutbox { get; } = usePessimisticModeForOutbox;

    public IsolationLevel IsolationLevel { get; } = isolationLevel;

    public bool UseTransactionScope { get; } = useTransactionScope;

    public System.Transactions.IsolationLevel ScopeIsolationLevel { get; } = scopeIsolationLevel;

    public override string ToString()
    {
        var outboxMode = UsePessimisticModeForOutbox ? "pessimistic" : "optimistic";
        var transaction = UseTransactionScope ? $"TransactionScope({ScopeIsolationLevel})" : $"Ado({IsolationLevel})";
        return $"{DatabaseEngine.GetType().Name}-Outbox({outboxMode})-{transaction}";
    }
}