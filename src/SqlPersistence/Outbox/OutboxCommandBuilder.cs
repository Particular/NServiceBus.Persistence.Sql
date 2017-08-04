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

        public static OutboxCommands Build(string tablePrefix, string schema, Type sqlVariant)
        {
            string tableName;
            if (sqlVariant == typeof(SqlDialect.MsSqlServer))
            {
                tableName = $"[{schema}].[{tablePrefix}OutboxData]";
            }
            else if (sqlVariant == typeof(SqlDialect.MySql))
            {
                tableName = $"`{tablePrefix}OutboxData`";
            }
            else if (sqlVariant == typeof(SqlDialect.Oracle))
            {
                tableName = $"{tablePrefix.ToUpper()}OD";
            }
            else
            {
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

        static string GetSetAsDispatchedCommand(Type sqlVariant, string tableName)
        {
            if (sqlVariant == typeof(SqlDialect.MsSqlServer) || sqlVariant == typeof(SqlDialect.MySql))
            {
                return $@"
update {tableName}
set
    Dispatched = 1,
    DispatchedAt = @DispatchedAt,
    Operations = '[]'
where MessageId = @MessageId";
            }
            if (sqlVariant == typeof(SqlDialect.Oracle))
            {
                return $@"
update ""{tableName}""
set
    Dispatched = 1,
    DispatchedAt = :DispatchedAt,
    Operations = '[]'
where MessageId = :MessageId";
            }

            throw new Exception($"Unknown SqlVariant: {sqlVariant}");
        }

        static string GetGetCommand(Type sqlVariant, string tableName)
        {
            if (sqlVariant == typeof(SqlDialect.MsSqlServer) || sqlVariant == typeof(SqlDialect.MySql))
            {
                return $@"
select
    Dispatched,
    Operations
from {tableName}
where MessageId = @MessageId";
            }
            if (sqlVariant == typeof(SqlDialect.Oracle))
            {
                return $@"
select
    Dispatched,
    Operations
from ""{tableName}""
where MessageId = :MessageId";
            }

            throw new Exception($"Unknown SqlVariant: {sqlVariant}");
        }

        static string GetStoreCommand(Type sqlVariant, string tableName)
        {
            if (sqlVariant == typeof(SqlDialect.MsSqlServer) || sqlVariant == typeof(SqlDialect.MySql))
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
            if (sqlVariant == typeof(SqlDialect.Oracle))
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

            throw new Exception($"Unknown SqlVariant: {sqlVariant}");
        }

        static string GetCleanupCommand(Type sqlVariant, string tableName)
        {
            if (sqlVariant == typeof(SqlDialect.MsSqlServer))
            {
                return $@"
delete top (@BatchSize) from {tableName}
where Dispatched = 'true'
    and DispatchedAt < @DispatchedBefore";
            }
            if (sqlVariant == typeof(SqlDialect.MySql))
            {
                return $@"
delete from {tableName}
where Dispatched = true
    and DispatchedAt < @DispatchedBefore
limit @BatchSize";
            }
            if (sqlVariant == typeof(SqlDialect.Oracle))
            {

                return $@"
delete from ""{tableName}""
where Dispatched = 1
    and DispatchedAt < :DispatchedBefore
    and rownum <= :BatchSize";
            }

            throw new Exception($"Unknown SqlVariant: {sqlVariant}");
        }
    }
}