namespace NServiceBus.Persistence.Sql
{
    public static class OutboxCommandBuilder
    {

        public static OutboxCommands Build(SqlVariant sqlVariant, string tablePrefix)
        {
            var tableName = $@"{tablePrefix}OutboxData";
            string storeCommandText = $@"
insert into {tableName}
(
    MessageId,
    Operations,
    PersistenceVersion
)
values
(
    @MessageId,
    @Operations,
    @PersistenceVersion
)";

            string cleanupCommandText = $@"
delete from {tableName} where Dispatched = true And DispatchedAt < @Date";

            string getCommandText = $@"
select
    Dispatched,
    Operations
from {tableName}
where MessageId = @MessageId";

            string setAsDispatchedCommandText = $@"
update {tableName}
set
    Dispatched = 1,
    DispatchedAt = @DispatchedAt,
    Operations = '[]'
where MessageId = @MessageId";
            return new OutboxCommands(
                store: storeCommandText,
                get: getCommandText,
                setAsDispatched: setAsDispatchedCommandText,
                cleanup: cleanupCommandText);
        }

    }
}