namespace NServiceBus
{
    public partial class SqlDialect
    {
        public partial class MsSqlServer
        {
            internal override string GetOutboxTableName(string tablePrefix)
            {
                return $"[{Schema}].[{tablePrefix}OutboxData]";
            }

            internal override string GetOutboxSetAsDispatchedCommand(string tableName)
            {
                return $@"
update {tableName}
set
    Dispatched = 1,
    DispatchedAt = @DispatchedAt,
    Operations = '[]'
where MessageId = @MessageId";
            }

            internal override string GetOutboxGetCommand(string tableName)
            {
                return $@"
select
    Dispatched,
    Operations
from {tableName}
where MessageId = @MessageId";
            }

            internal override string GetOutboxOptimisticStoreCommand(string tableName)
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

            internal override string GetOutboxPessimisticBeginCommand(string tableName)
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
    '[]',
    @PersistenceVersion
)";
            }

            internal override string GetOutboxPessimisticCompleteCommand(string tableName)
            {
                return $@"
update {tableName}
set
    Operations = @Operations
where MessageId = @MessageId";
            }

            internal override string GetOutboxCleanupCommand(string tableName)
            {
                return $@"
delete top (@BatchSize) from {tableName}
where Dispatched = 'true' and
      DispatchedAt < @DispatchedBefore";
            }

            internal override string AddOutboxPadding(string json)
            {
                //We need to ensure the outbox content is at lest 8000 bytes long because otherwise SQL Server will attempt to
                //store is inside the data page which will result in low space utilization.

We tried using *varchar values of of the row* table option but while it did improve situation on on-premises SQL Server it didn't work as expected in SQL Azure where it caused LOB pages to be allocated (one for each record) but never deallocated after the messages data is supposed to be removed.
                //We use 4000 instead of 8000 in the condition because the SQL Persistence uses nvarchar data type which encodes
                //strings at UTF-16 (2 bytes per character)

                //We allow content smaller than 1800 characters (3600 bytes) to not be padded because such content does not block
                //SQL Server from re-using data pages.
                if (json.Length >= 1800 && json.Length <= 4000)
                {
                    return json.PadRight(4000, ' ');
                }

                return json;
            }
        }
    }
}
