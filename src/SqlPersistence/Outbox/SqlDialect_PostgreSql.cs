namespace NServiceBus
{
    public partial class SqlDialect
    {
        public partial class PostgreSql
        {
            internal override string GetOutboxTableName(string tablePrefix)
            {
                return $"{tablePrefix}OutboxData";
            }

            internal override string GetOutboxSetAsDispatchedCommand(string tableName)
            {
                return $@"
update public.""{tableName}""
set
    ""Dispatched"" = true,
    ""DispatchedAt"" = @DispatchedAt at time zone 'UTC',
    ""Operations"" = '[]'
where ""MessageId"" = @MessageId";
            }

            internal override string GetOutboxGetCommand(string tableName)
            {
                return $@"
select
    ""Dispatched"",
    ""Operations""
from public.""{tableName}""
where ""MessageId"" = @MessageId";
            }

            internal override string GetOutboxStoreCommand(string tableName)
            {
                return $@"
insert into public.""{tableName}""
(
    ""MessageId"",
    ""Operations"",
    ""PersistenceVersion""
)
values
(
    @MessageId,
    @Operations,
    @PersistenceVersion
)";
            }

            internal override string GetOutboxCleanupCommand(string tableName)
            {
                return $@"
delete from public.""{tableName}""
where ctid in
(
    select ctid
    from public.""{tableName}""
    where
        ""Dispatched"" = true and
        ""DispatchedAt"" < @DispatchedBefore
    limit @BatchSize
)";
            }
        }
    }
}