declare @outboxTableName nvarchar(max) = '[' + @schema + '].[' + @tablePrefix + 'OutboxData]';
declare @inboxTableName nvarchar(max) = '[' + @schema + '].[' + @tablePrefix + 'InboxData]';

--Outbox table
if not exists (
    select * from sys.objects
    where
        object_id = object_id(@outboxTableName)
        and type in ('U')
)
begin
declare @createOutboxTable nvarchar(max);
set @createOutboxTable = '
    create table ' + @outboxTableName + '(
        MessageId nvarchar(200) not null primary key nonclustered,
        PersistenceVersion varchar(23) not null,
        Operations nvarchar(max) not null
    )
';
exec(@createOutboxTable);
end

--Inbox table
if not exists (
    select * from sys.objects
    where
        object_id = object_id(@inboxTableName)
        and type in ('U')
)
begin
declare @createInboxTable nvarchar(max);
set @createInboxTable = '
    create table ' + @inboxTableName + '(
        MessageId nvarchar(200) not null,
        Version timestamp not null
    )
';
exec(@createInboxTable);
end

--Inbox indexes
if not exists
(
    select *
    from sys.indexes
    where
        name = 'Index_MessageId' and
        object_id = object_id(@inboxTableName)
)
begin
  declare @createMessageIdIndex nvarchar(max);
  set @createMessageIdIndex = '
  create unique index Index_MessageId
  on ' + @inboxTableName + '(MessageId);';
  exec(@createMessageIdIndex);
end


if not exists
(
    select *
    from sys.indexes
    where
        name = 'Index_Version' and
        object_id = object_id(@inboxTableName)
)
begin
  declare @createVersionIndex nvarchar(max);
  set @createVersionIndex = '
  create index Index_Version
  on ' + @inboxTableName + '([Version]);';
  exec(@createVersionIndex);
end

--Prefil Inbox

declare @prefilInbox nvarchar(max);
set @prefilInbox = '
    declare @count int = (select count(*) from ' + @inboxTableName + ');
	declare @target int = ' + CONVERT(varchar(20), @inboxRowCount) + ';
	declare @index int = @count;

	while @index < @target
	begin
		insert into ' + @inboxTableName + ' (MessageId) values (''_'' + CAST(NEWID() AS NVARCHAR(36)))
		set @index = @index + 1;
	end
';
exec(@prefilInbox);
