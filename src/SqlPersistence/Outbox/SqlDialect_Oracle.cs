namespace NServiceBus
{
    public partial class SqlDialect
    {
        public partial class Oracle
        {
            internal override string GetOutboxTableName(string tablePrefix)
            {
                return $"{tablePrefix.ToUpper()}OD";
            }

            internal override string GetOutboxSetAsDispatchedCommand(string tableName)
            {
                return $@"
update ""{tableName}""
set
    Dispatched = 1,
    DispatchedAt = :DispatchedAt,
    Operations = '[]'
where MessageId = :MessageId";
            }

            internal override string GetOutboxGetCommand(string tableName)
            {
                return $@"
select
    Dispatched,
    Operations
from ""{tableName}""
where MessageId = :MessageId";
            }

            internal override string GetOutboxStoreCommand(string tableName)
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

            internal override string GetOutboxCleanupCommand(string tableName)
            {
                return $@"
delete from ""{tableName}""
where Dispatched = 1
    and DispatchedAt < :DispatchedBefore
    and rownum <= :BatchSize";
            }
        }
    }
}