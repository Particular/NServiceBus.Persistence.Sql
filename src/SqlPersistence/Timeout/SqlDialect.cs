namespace NServiceBus
{
    public partial class SqlDialect
    {
        internal abstract string GetTimeoutTableName(string tablePrefix);
        internal abstract string GetTimeoutGetCommand(string tableName);
        internal abstract string GetTimeoutRequestCommand(string tableName);
        internal abstract string GetTimeoutRescheduleCommand(string tableName);
        internal abstract string GetTimeoutRemoveCommand(string tableName);
        internal abstract string GetTimeoutRemoveBySagaIdCommand(string tableName);
    }
}
