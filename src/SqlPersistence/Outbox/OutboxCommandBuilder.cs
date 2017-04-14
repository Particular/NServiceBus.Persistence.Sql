#pragma warning disable 1591
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

            var cleanupCommand = GetCleanupCommand(sqlVariant, tableName);

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

        static string GetCleanupCommand(SqlVariant sqlVariant, string tableName)
        {
            switch (sqlVariant)
            {
                case SqlVariant.MsSqlServer:
                    return $@"
delete top (@BatchSize) from {tableName}
where Dispatched = 'true'
    and DispatchedAt < @Date";
                case SqlVariant.MySql:
                    return $@"
delete from {tableName}
where Dispatched = true
    and DispatchedAt < @Date
limit @BatchSize";
                default:
                    throw new Exception($"Unknown SqlVariant: {sqlVariant}");
            }
        }
    }
}