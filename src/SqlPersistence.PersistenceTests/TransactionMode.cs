namespace NServiceBus.PersistenceTesting;

public abstract record TransactionMode
{
    public static AdoTransactionMode Ado(System.Data.IsolationLevel isolationLevel) => new(isolationLevel);
    public static TransactionScopeMode Scope(System.Transactions.IsolationLevel isolationLevel) => new(isolationLevel);
}

public record TransactionScopeMode(System.Transactions.IsolationLevel IsolationLevel) : TransactionMode
{
}

public record AdoTransactionMode(System.Data.IsolationLevel IsolationLevel) : TransactionMode
{
}