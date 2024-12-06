namespace NServiceBus.PersistenceTesting;

record SqlTestVariant(DatabaseEngine DatabaseEngine,
    TransactionMode TransactionMode,
    OutboxLockMode OutboxLockMode)
{
    public override string ToString() => $"{DatabaseEngine}-{TransactionMode}-{OutboxLockMode}";
}