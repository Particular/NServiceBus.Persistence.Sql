namespace NServiceBus.PersistenceTesting;

class SqlTestVariant(DatabaseEngine databaseEngine,
    TransactionMode transactionMode,
    bool usePessimisticModeForOutbox)
{
    public DatabaseEngine DatabaseEngine { get; } = databaseEngine;

    public TransactionMode TransactionMode { get; } = transactionMode;

    public bool UsePessimisticModeForOutbox { get; } = usePessimisticModeForOutbox;

    public override string ToString()
    {
        var outboxMode = UsePessimisticModeForOutbox ? "pessimistic" : "optimistic";
        return $"{DatabaseEngine}-{TransactionMode}-{outboxMode}";
    }
}