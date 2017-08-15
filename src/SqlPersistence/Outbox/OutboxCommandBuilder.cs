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

        public static OutboxCommands Build(string tablePrefix, SqlDialect sqlDialect)
        {
            string tableName;
            if (sqlDialect is SqlDialect.MsSqlServer)
            {
                tableName = $"[{sqlDialect.Schema}].[{tablePrefix}OutboxData]";
            }
            else if (sqlDialect is SqlDialect.MySql)
            {
                tableName = $"`{tablePrefix}OutboxData`";
            }
            else if (sqlDialect is SqlDialect.Oracle)
            {
                tableName = $"{tablePrefix.ToUpper()}OD";
            }
            else
            {
                throw new Exception($"Unknown SqlDialect: {sqlDialect.Name}");
            }

            var storeCommandText = GetStoreCommand(sqlDialect, tableName);

            var cleanupCommand = GetCleanupCommand(sqlDialect, tableName);

            var getCommandText = GetGetCommand(sqlDialect, tableName);

            var setAsDispatchedCommand = GetSetAsDispatchedCommand(sqlDialect, tableName);

            return new OutboxCommands(
                store: storeCommandText,
                get: getCommandText,
                setAsDispatched: setAsDispatchedCommand,
                cleanup: cleanupCommand);
        }

        static string GetSetAsDispatchedCommand(SqlDialect sqlDialect, string tableName)
        {
            if (sqlDialect is SqlDialect.MsSqlServer || sqlDialect is SqlDialect.MySql)
            {
                return $@"
update {tableName}
set
    Dispatched = 1,
    DispatchedAt = @DispatchedAt,
    Operations = '[]'
where MessageId = @MessageId";
            }
            if (sqlDialect is SqlDialect.Oracle)
            {
                return $@"
update ""{tableName}""
set
    Dispatched = 1,
    DispatchedAt = :DispatchedAt,
    Operations = '[]'
where MessageId = :MessageId";
            }

            throw new Exception($"Unknown SqlDialect: {sqlDialect}");
        }

        static string GetGetCommand(SqlDialect sqlDialect, string tableName)
        {
            if (sqlDialect is SqlDialect.MsSqlServer || sqlDialect is SqlDialect.MySql)
            {
                return $@"
select
    Dispatched,
    Operations
from {tableName}
where MessageId = @MessageId";
            }
            if (sqlDialect is SqlDialect.Oracle)
            {
                return $@"
select
    Dispatched,
    Operations
from ""{tableName}""
where MessageId = :MessageId";
            }

            throw new Exception($"Unknown SqlDialect: {sqlDialect}");
        }

        static string GetStoreCommand(SqlDialect sqlDialect, string tableName)
        {
            if (sqlDialect is SqlDialect.MsSqlServer || sqlDialect is SqlDialect.MySql)
            {
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
            }
            if (sqlDialect is SqlDialect.Oracle)
            {
                return $@"
insert into ""{tableName}""
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
            }

            throw new Exception($"Unknown SqlDialect: {sqlDialect.Name}");
        }

        static string GetCleanupCommand(SqlDialect sqlDialect, string tableName)
        {
            if (sqlDialect is SqlDialect.MsSqlServer)
            {
                return $@"
delete top (@BatchSize) from {tableName}
where Dispatched = 'true'
    and DispatchedAt < @DispatchedBefore";
            }
            if (sqlDialect is SqlDialect.MySql)
            {
                return $@"
delete from {tableName}
where Dispatched = true
    and DispatchedAt < @DispatchedBefore
limit @BatchSize";
            }
            if (sqlDialect is SqlDialect.Oracle)
            {

                return $@"
delete from ""{tableName}""
where Dispatched = 1
    and DispatchedAt < :DispatchedBefore
    and rownum <= :BatchSize";
            }

            throw new Exception($"Unknown SqlDialect: {sqlDialect.Name}");
        }
    }
}