namespace NServiceBus.Persistence.Sql
{
    using System;

    /// <summary>
    /// Not for public use.
    /// </summary>
    [Obsolete("Not for public use")]
    public static class OutboxCommandBuilder
    {

        public static OutboxCommands Build(string tablePrefix, string schema, SqlVariant sqlVariant)
        {
            string inboxTableName;
            string outboxTableName;

            switch (sqlVariant)
            {
                case SqlVariant.MsSqlServer:
                    outboxTableName = $"[{schema}].[{tablePrefix}OutboxData]";
                    inboxTableName = $"[{schema}].[{tablePrefix}InboxData]";
                    break;
                case SqlVariant.MySql:
                    outboxTableName = $"`{tablePrefix}OutboxData`";
                    inboxTableName = $"`{tablePrefix}InboxData`";
                    break;
                default:
                    throw new Exception($"Unknown SqlVariant: {sqlVariant}");
            }

            var storeCommandText = $@"
with InboxSlot as 
    (select top(1) * from {inboxTableName} with (updlock, readpast, rowlock) order by [Version])
update InboxSlot set MessageId = @MessageId;

if @@ROWCOUNT = 0
begin
   throw 50000, 'Cannot claim inbox slot', 0
end

insert into {outboxTableName}
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

            var getCommandText = $@"
select Operations from {outboxTableName} where MessageId = @MessageId
union all
select null from {inboxTableName} where MessageId = @MessageId";

            var setAsDispatchedCommand = $@"
delete from {outboxTableName}
where MessageId = @MessageId";

            return new OutboxCommands(
                store: storeCommandText,
                get: getCommandText,
                setAsDispatched: setAsDispatchedCommand);
        }
    }
}