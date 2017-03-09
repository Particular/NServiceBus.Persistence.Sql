namespace NServiceBus.Persistence.Sql
{
    using System;

    /// <summary>
    /// Not for public use.
    /// </summary>
    [Obsolete("Not for public use")]
    public static class OutboxCommandBuilder
    {

        public static OutboxCommands Build(string tablePrefix, string schema, SqlVariant sqlVariant)
        {
            string tableName;
            switch (sqlVariant)
            {
                case SqlVariant.MsSqlServer:
                    tableName = $"[{schema}].[{tablePrefix}OutboxData]";
                    break;
                case SqlVariant.MySql:
                    tableName = $"`{tablePrefix}OutboxData`";
                    break;
                default: 
                    throw new Exception($"Unknown SqlVariant: {sqlVariant}");
            }

            var storeCommandText = $@"
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

            var cleanupCommand = $@"
delete from {tableName} where Dispatched = true And DispatchedAt < @Date";

            var getCommandText = $@"
select
    Dispatched,
    Operations
from {tableName}
where MessageId = @MessageId";

            var setAsDispatchedCommand = $@"
update {tableName}
set
    Dispatched = 1,
    DispatchedAt = @DispatchedAt,
    Operations = '[]'
where MessageId = @MessageId";
            return new OutboxCommands(
                store: storeCommandText,
                get: getCommandText,
                setAsDispatched: setAsDispatchedCommand,
                cleanup: cleanupCommand);
        }

    }
}