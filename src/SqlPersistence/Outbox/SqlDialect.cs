namespace NServiceBus
{
    public partial class SqlDialect
    {
        internal abstract string GetOutboxTableName(string tablePrefix);
        internal abstract string GetOutboxSetAsDispatchedCommand(string tableName);
        internal abstract string GetOutboxGetCommand(string tableName);
        internal abstract string GetOutboxStoreCommand(string tableName);
        internal abstract string GetOutboxCleanupCommand(string tableName);
    }
}