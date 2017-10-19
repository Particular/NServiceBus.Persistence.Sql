namespace NServiceBus
{
    using System;
    using System.Data.SqlTypes;

    public partial class SqlDialect
    {
        public partial class MsSqlServer
        {
            internal override object ConvertTimeoutId(string timeoutId)
            {
                if (Guid.TryParse(timeoutId, out var guid))
                {
                    return guid;
                }
                throw new Exception($"Expected timeoutId to be in GUID format: {timeoutId}");
            }

            internal override DateTime OldestSupportedTimeout => SqlDateTime.MinValue.Value;

            internal override string GetTimeoutTableName(string tablePrefix)
            {
                return $"[{Schema}].[{tablePrefix}TimeoutData]";
            }

            internal override string GetTimeoutInsertCommand(string tableName)
            {
                return $@"
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
            }

            internal override string GetTimeoutRemoveByIdCommand(string tableName)
            {
                return $@"
delete from {tableName}
where Id = @Id";
            }

            internal override string GetTimeoutRemoveBySagaIdCommand(string tableName)
            {
                return $@"
delete from {tableName}
where SagaId = @SagaId";
            }

            internal override string GetTimeoutPeekCommand(string tableName)
            {
                return $@"
select
    Destination,
    SagaId,
    State,
    Time,
    Headers
from {tableName}
where Id = @Id";
            }

            internal override string GetTimeoutRangeCommand(string tableName)
            {
                return $@"
select Id, Time
from {tableName}
where Time > @StartTime and Time <= @EndTime";
            }

            internal override string GetTimeoutNextCommand(string tableName)
            {
                return $@"
select top 1 Time from {tableName}
where Time > @EndTime
order by Time";
            }
        }
    }
}