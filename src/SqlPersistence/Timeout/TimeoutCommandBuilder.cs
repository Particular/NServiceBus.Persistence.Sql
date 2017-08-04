using System;
#pragma warning disable 1591

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// Not for public use.
    /// </summary>
    [Obsolete("Not for public use")]
    public static class TimeoutCommandBuilder
    {

        public static TimeoutCommands Build(Type sqlVariant, string tablePrefix, string schema)
        {
            if (sqlVariant == typeof(SqlDialect.MySql))
            {
                return BuildMySqlCommands($"`{tablePrefix}TimeoutData`");
            }
            if (sqlVariant == typeof(SqlDialect.MsSqlServer))
            {
                return BuildSqlServerCommands($"[{schema}].[{tablePrefix}TimeoutData]");
            }
            if (sqlVariant == typeof(SqlDialect.Oracle))
            {
                return BuildOracleCommands($"{tablePrefix.ToUpper()}TO");
            }

            throw new Exception($"Unknown SqlVariant: {sqlVariant}.");
        }

        static TimeoutCommands BuildMySqlCommands(string tableName)
        {
            var insertCommandText = $@"
insert into {tableName}
(
    Id,
    Destination,
    SagaId,
    State,
    Time,
    Headers,
    PersistenceVersion
)
values
(
    @Id,
    @Destination,
    @SagaId,
    @State,
    @Time,
    @Headers,
    @PersistenceVersion
)";

            var removeByIdCommandText = $@"
delete from {tableName}
where Id = @Id;";

            var removeBySagaIdCommandText = $@"
delete from {tableName}
where SagaId = @SagaId";

            var selectByIdCommandText = $@"
select
    Destination,
    SagaId,
    State,
    Time,
    Headers
from {tableName}
where Id = @Id";

            var rangeCommandText = $@"
select Id, Time
from {tableName}
where Time > @StartTime and Time <= @EndTime";

            var nextCommandText = $@"
select Time from {tableName}
where Time > @EndTime
order by Time
limit 1";
            return new TimeoutCommands
            (
                next: nextCommandText,
                range: rangeCommandText,
                peek: selectByIdCommandText,
                removeBySagaId: removeBySagaIdCommandText,
                removeById: removeByIdCommandText,
                add: insertCommandText
            );
        }

        static TimeoutCommands BuildSqlServerCommands(string tableName)
        {
            var insertCommandText = $@"
insert into {tableName}
(
    Id,
    Destination,
    SagaId,
    State,
    Time,
    Headers,
    PersistenceVersion
)
values
(
    @Id,
    @Destination,
    @SagaId,
    @State,
    @Time,
    @Headers,
    @PersistenceVersion
)";

            var removeByIdCommandText = $@"
delete from {tableName}
where Id = @Id";


            var removeBySagaIdCommandText = $@"
delete from {tableName}
where SagaId = @SagaId";

            var selectByIdCommandText = $@"
select
    Destination,
    SagaId,
    State,
    Time,
    Headers
from {tableName}
where Id = @Id";

            var rangeCommandText = $@"
select Id, Time
from {tableName}
where Time > @StartTime and Time <= @EndTime";

            var nextCommandText = $@"
select top 1 Time from {tableName}
where Time > @EndTime
order by Time";

            return new TimeoutCommands
            (
                next: nextCommandText,
                range: rangeCommandText,
                peek: selectByIdCommandText,
                removeBySagaId: removeBySagaIdCommandText,
                removeById: removeByIdCommandText,
                add: insertCommandText
            );
        }

        static TimeoutCommands BuildOracleCommands(string tableName)
        {
            var insertCommandText = $@"
insert into ""{tableName}""
(
    Id,
    Destination,
    SagaId,
    State,
    ExpireTime,
    Headers,
    PersistenceVersion
)
values
(
    :Id,
    :Destination,
    :SagaId,
    :State,
    :Time,
    :Headers,
    :PersistenceVersion
)";

            var removeByIdCommandText = $@"
delete from ""{tableName}""
where Id = :Id";


            var removeBySagaIdCommandText = $@"
delete from ""{tableName}""
where SagaId = :SagaId";

            var selectByIdCommandText = $@"
select
    Destination,
    SagaId,
    State,
    ExpireTime,
    Headers
from ""{tableName}""
where Id = :Id";

            var rangeCommandText = $@"
select Id, ExpireTime
from ""{tableName}""
where ExpireTime > :StartTime and ExpireTime <= :EndTime";

            var nextCommandText = $@"
select ExpireTime
from
(
    select ExpireTime from ""{tableName}""
    where ExpireTime > :EndTime
    order by ExpireTime
) subquery
where rownum <= 1";

            return new TimeoutCommands
            (
                next: nextCommandText,
                range: rangeCommandText,
                peek: selectByIdCommandText,
                removeBySagaId: removeBySagaIdCommandText,
                removeById: removeByIdCommandText,
                add: insertCommandText
            );
        }
    }
}