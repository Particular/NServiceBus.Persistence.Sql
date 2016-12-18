using System;

namespace NServiceBus.Persistence.Sql
{
    public static class TimeoutCommandBuilder
    {

        public static TimeoutCommands Build(SqlVarient sqlVarient, string tablePrefix)
        {
            var tableName = $@"{tablePrefix}TimeoutData";
            switch (sqlVarient)
            {
                case SqlVarient.MsSqlServer:
                    return BuildSqlServerCommands(tableName);
                case SqlVarient.MySql:
                    return BuildMySqlCommands(tableName);
                default:
                    throw new Exception($"Unknown SqlVarient: {sqlVarient}.");
            }
        }

        static TimeoutCommands BuildMySqlCommands(string tableName)
        {
            string insertCommandText = $@"
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

            string removeByIdCommandText = $@"
set @sagaId := (select SagaId from {tableName} where Id = @Id);
delete from {tableName}
where Id = @Id;
select @sagaId;";


            string removeBySagaIdCommandText = $@"
delete from {tableName}
where SagaId = @SagaId";

            string selectByIdCommandText = $@"
select
    Destination,
    SagaId,
    State,
    Time,
    Headers
from {tableName}
where Id = @Id";

            string rangeComandText = $@"
select Id, Time
from {tableName}
where Time between @StartTime and @EndTime";

            string nextCommandText = $@"
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
            string insertCommandText = $@"
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

            string removeByIdCommandText = $@"
delete from {tableName}
output deleted.SagaId
where Id = @Id";


            string removeBySagaIdCommandText = $@"
delete from {tableName}
where SagaId = @SagaId";

            string selectByIdCommandText = $@"
select
    Destination,
    SagaId,
    State,
    Time,
    Headers
from {tableName}
where Id = @Id";

            string rangeComandText = $@"
select Id, Time
from {tableName}
where Time between @StartTime and @EndTime";

            string nextCommandText = $@"
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