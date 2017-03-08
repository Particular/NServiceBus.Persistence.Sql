using System;

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// Not for public use.
    /// </summary>
    [Obsolete("Not for public use")]
    public static class TimeoutCommandBuilder
    {

        public static TimeoutCommands Build(SqlVariant sqlVariant, string tablePrefix, string schema)
        {
            switch (sqlVariant)
            {
                case SqlVariant.MySql:
                    return BuildMySqlCommands($"`{tablePrefix}TimeoutData`");
                case SqlVariant.MsSqlServer:
                    return BuildSqlServerCommands($"[{schema}].[{tablePrefix}TimeoutData]");
                default:
                    throw new Exception($"Unknown SqlVariant: {sqlVariant}.");
            }
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
set @sagaId := (select SagaId from {tableName} where Id = @Id);
delete from {tableName}
where Id = @Id;
select @sagaId;";

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

            var rangeComandText = $@"
select Id, Time
from {tableName}
where Time between @StartTime and @EndTime";

            var nextCommandText = $@"
select Time from {tableName}
where Time > @EndTime
order by Time
limit 1";
            return new TimeoutCommands
            (
                next: nextCommandText,
                range: rangeComandText,
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
output deleted.SagaId
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

            var rangeComandText = $@"
select Id, Time
from {tableName}
where Time between @StartTime and @EndTime";

            var nextCommandText = $@"
select top 1 Time from {tableName}
where Time > @EndTime
order by Time";

            return new TimeoutCommands
            (
                next: nextCommandText,
                range: rangeComandText,
                peek: selectByIdCommandText,
                removeBySagaId: removeBySagaIdCommandText,
                removeById: removeByIdCommandText,
                add: insertCommandText
            );
        }
    }
}