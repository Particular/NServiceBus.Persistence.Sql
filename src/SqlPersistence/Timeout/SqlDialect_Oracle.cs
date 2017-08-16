namespace NServiceBus
{
    using System;

    public partial class SqlDialect
    {
        public partial class Oracle
        {
            internal override DateTime OldestSupportedTimeout => new DateTime(1000, 1, 1);

            internal override string GetTimeoutTableName(string tablePrefix)
            {
               return $"{tablePrefix.ToUpper()}TO";
            }

            internal override string GetTimeoutInsertCommand(string tableName)
            {
                return $@"
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
            }

            internal override string GetTimeoutRemoveByIdCommand(string tableName)
            {
                return $@"
delete from ""{tableName}""
where Id = :Id";
            }

            internal override string GetTimeoutRemoveBySagaIdCommand(string tableName)
            {
                return $@"
delete from ""{tableName}""
where SagaId = :SagaId";
            }

            internal override string GetTimeoutPeekCommand(string tableName)
            {
                return $@"
select
    Destination,
    SagaId,
    State,
    ExpireTime,
    Headers
from ""{tableName}""
where Id = :Id";
            }

            internal override string GetTimeoutRangeCommand(string tableName)
            {
                return $@"
select Id, ExpireTime
from ""{tableName}""
where ExpireTime > :StartTime and ExpireTime <= :EndTime";
            }

            internal override string GetTimeoutNextCommand(string tableName)
            {
                return $@"
select ExpireTime
from
(
    select ExpireTime from ""{tableName}""
    where ExpireTime > :EndTime
    order by ExpireTime
) subquery
where rownum <= 1";
            }
        }
    }
}