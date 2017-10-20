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
                case SqlVariant.Oracle:
                    tableName = string.IsNullOrEmpty(schema) ? $"\"{tablePrefix.ToUpper()}OD\"" : $"\"{schema}\".\"{tablePrefix.ToUpper()}OD\"";
                    break;
                default:
                    throw new Exception($"Unknown SqlVariant: {sqlVariant}");
            }

            var storeCommandText = GetStoreCommand(sqlVariant, tableName);

            var cleanupCommand = GetCleanupCommand(sqlVariant, tableName);

            var getCommandText = GetGetCommand(sqlVariant, tableName);

            var setAsDispatchedCommand = GetSetAsDispatchedCommand(sqlVariant, tableName);

            return new OutboxCommands(
                store: storeCommandText,
                get: getCommandText,
                setAsDispatched: setAsDispatchedCommand,
                cleanup: cleanupCommand);
        }

        static string GetSetAsDispatchedCommand(SqlVariant sqlVariant, string tableName)
        {
            switch (sqlVariant)
            {
                case SqlVariant.MsSqlServer:
                case SqlVariant.MySql:
                    return $@"
update {tableName}
set
    Dispatched = 1,
    DispatchedAt = @DispatchedAt,
    Operations = '[]'
where MessageId = @MessageId";
                case SqlVariant.Oracle:
                    return $@"
update {tableName}
set
    Dispatched = 1,
    DispatchedAt = :DispatchedAt,
    Operations = '[]'
where MessageId = :MessageId";
                default:
                    throw new Exception($"Unknown SqlVariant: {sqlVariant}");
            }
        }

        static string GetGetCommand(SqlVariant sqlVariant, string tableName)
        {
            switch (sqlVariant)
            {
                case SqlVariant.MsSqlServer:
                case SqlVariant.MySql:
                    return $@"
select
    Dispatched,
    Operations
from {tableName}
where MessageId = @MessageId";
                case SqlVariant.Oracle:
                    return $@"
select
    Dispatched,
    Operations
from {tableName}
where MessageId = :MessageId";
                default:
                    throw new Exception($"Unknown SqlVariant: {sqlVariant}");
            }
        }

        static string GetStoreCommand(SqlVariant sqlVariant, string tableName)
        {
            switch (sqlVariant)
            {
                case SqlVariant.MsSqlServer:
                case SqlVariant.MySql:
                    return $@"
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
                case SqlVariant.Oracle:
                    return $@"
insert into {tableName}
(
    MessageId,
    Operations,
    PersistenceVersion
)
values
(
    :MessageId,
    :Operations,
    :PersistenceVersion
)";
                default:
                    throw new Exception($"Unknown SqlVariant: {sqlVariant}");
            }

        }

        static string GetCleanupCommand(SqlVariant sqlVariant, string tableName)
        {
            switch (sqlVariant)
            {
                case SqlVariant.MsSqlServer:
                    return $@"
delete top (@BatchSize) from {tableName}
where Dispatched = 'true'
    and DispatchedAt < @DispatchedBefore";
                case SqlVariant.MySql:
                    return $@"
delete from {tableName}
where Dispatched = true
    and DispatchedAt < @DispatchedBefore
limit @BatchSize";
                case SqlVariant.Oracle:
                    return $@"
delete from {tableName}
where Dispatched = 1
    and DispatchedAt < :DispatchedBefore
    and rownum <= :BatchSize";
                default:
                    throw new Exception($"Unknown SqlVariant: {sqlVariant}");
            }
        }
    }
}