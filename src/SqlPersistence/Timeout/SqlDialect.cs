namespace NServiceBus
{
    using System;

    public partial class SqlDialect
    {
        internal abstract DateTime OldestSupportedTimeout { get; }
        internal abstract string GetTimeoutTableName(string tablePrefix);
        internal abstract string GetTimeoutInsertCommand(string tableName);
        internal abstract string GetTimeoutRemoveByIdCommand(string tableName);
        internal abstract string GetTimeoutRemoveBySagaIdCommand(string tableName);
        internal abstract string GetTimeoutPeekCommand(string tableName);
        internal abstract string GetTimeoutRangeCommand(string tableName);
        internal abstract string GetTimeoutNextCommand(string tableName);
    }
}