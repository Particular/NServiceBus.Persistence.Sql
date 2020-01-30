using NServiceBus;

static class OutboxCommandBuilder
{
    public static OutboxCommands Build(string tablePrefix, SqlDialect sqlDialect)
    {
        var tableName = sqlDialect.GetOutboxTableName(tablePrefix);

        var storeCommandText = sqlDialect.GetOutboxStoreCommand(tableName);

        var cleanupCommand = sqlDialect.GetOutboxCleanupCommand(tableName);

        var getCommandText = sqlDialect.GetOutboxGetCommand(tableName);

        var setAsDispatchedCommand = sqlDialect.GetOutboxSetAsDispatchedCommand(tableName);

        return new OutboxCommands(
            store: storeCommandText,
            get: getCommandText,
            setAsDispatched: setAsDispatchedCommand,
            cleanup: cleanupCommand);
    }
}