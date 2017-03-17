declare @outboxTableName nvarchar(max) = '[' + @schema + '].[' + @tablePrefix + 'OutboxData]';
declare @inboxTableName nvarchar(max) = '[' + @schema + '].[' + @tablePrefix + 'InboxData]';

if exists
(
    select *
    from sys.objects
    where
        object_id = object_id(@outboxTableName) and
        type in ('U')
)
begin
declare @dropOutboxTable nvarchar(max);
set @dropOutboxTable = 'drop table ' + @outboxTableName;
exec(@dropOutboxTable);
end

if exists
(
    select *
    from sys.objects
    where
        object_id = object_id(@inboxTableName) and
        type in ('U')
)
begin
declare @dropInboxTable nvarchar(max);
set @dropInboxTable = 'drop table ' + @inboxTableName;
exec(@dropInboxTable);
end