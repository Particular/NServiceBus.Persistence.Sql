namespace NServiceBus
{
    public partial class SqlDialect
    {
        internal abstract string GetOutboxTableName(string tablePrefix);
        internal abstract string GetOutboxSetAsDispatchedCommand(string tableName);
        internal abstract string GetOutboxGetCommand(string tableName);
        internal abstract string GetOutboxOptimisticStoreCommand(string tableName);
        internal abstract string GetOutboxPessimisticBeginCommand(string tableName);
        internal abstract string GetOutboxPessimisticCompleteCommand(string tableName);
        internal abstract string GetOutboxCleanupCommand(string tableName);
        internal virtual string AddOutboxPadding(string json) => json;
    }
}