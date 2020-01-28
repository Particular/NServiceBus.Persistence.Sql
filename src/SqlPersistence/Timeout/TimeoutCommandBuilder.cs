using NServiceBus;

static class TimeoutCommandBuilder
{
    public static TimeoutCommands Build(SqlDialect sqlDialect, string tablePrefix)
    {
        var tableName = sqlDialect.GetTimeoutTableName(tablePrefix);

        return new TimeoutCommands(
            removeById: sqlDialect.GetTimeoutRemoveByIdCommand(tableName),
            next: sqlDialect.GetTimeoutNextCommand(tableName),
            peek: sqlDialect.GetTimeoutPeekCommand(tableName),
            add: sqlDialect.GetTimeoutInsertCommand(tableName),
            removeBySagaId: sqlDialect.GetTimeoutRemoveBySagaIdCommand(tableName),
            range: sqlDialect.GetTimeoutRangeCommand(tableName)
        );
    }
}