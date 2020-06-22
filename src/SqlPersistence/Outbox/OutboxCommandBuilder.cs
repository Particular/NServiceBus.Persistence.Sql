using NServiceBus;

// used by docs engine to create scripts
static class OutboxCommandBuilder
{
    public static OutboxCommands Build(SqlDialect sqlDialect, string tablePrefix)
    {
        var tableName = sqlDialect.GetOutboxTableName(tablePrefix);

        var optimisticStoreCommandText = sqlDialect.GetOutboxOptimisticStoreCommand(tableName);
        var pessimisticBeginCommandText = sqlDialect.GetOutboxPessimisticBeginCommand(tableName);
        var pessimisticCompleteCommandText = sqlDialect.GetOutboxPessimisticCompleteCommand(tableName);

        var cleanupCommand = sqlDialect.GetOutboxCleanupCommand(tableName);

        var getCommandText = sqlDialect.GetOutboxGetCommand(tableName);

        var setAsDispatchedCommand = sqlDialect.GetOutboxSetAsDispatchedCommand(tableName);

        return new OutboxCommands(
            optimisticStoreCommandText, 
            pessimisticBeginCommandText, 
            pessimisticCompleteCommandText, 
            getCommandText, 
            setAsDispatchedCommand, 
            cleanupCommand);
    }
}