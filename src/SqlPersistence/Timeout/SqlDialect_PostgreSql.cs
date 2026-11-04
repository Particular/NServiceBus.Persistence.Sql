namespace NServiceBus
{
    public partial class SqlDialect
    {
        public partial class PostgreSql
        {
            internal override string GetTimeoutTableName(string tablePrefix) =>
                $"\"{Schema}\".\"{tablePrefix}SagaTimeoutData\"";

            internal override string GetTimeoutGetCommand(string tableName) => $@"
select ""Data""
from {tableName}
where ""SagaId"" = @SagaId
order by ""TimeoutId""";

            internal override string GetTimeoutRequestCommand(string tableName) => $@"
insert into {tableName}
(
    ""SagaId"",
    ""TimeoutId"",
    ""Data"",
    ""PersistenceVersion""
)
values
(
    @SagaId,
    @TimeoutId,
    @Data,
    @PersistenceVersion
)";

            internal override string GetTimeoutRescheduleCommand(string tableName) => $@"
update {tableName}
set ""Data"" = @Data
where ""SagaId"" = @SagaId and ""TimeoutId"" = @TimeoutId";

            internal override string GetTimeoutRemoveCommand(string tableName) => $@"
delete from {tableName}
where ""SagaId"" = @SagaId and ""TimeoutId"" = @TimeoutId";

            internal override string GetTimeoutRemoveBySagaIdCommand(string tableName) => $@"
delete from {tableName}
where ""SagaId"" = @SagaId";
        }
    }
}
