declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + 'OutboxData]';

if not exists (
    select * from sys.objects
    where
        object_id = object_id(@tableName)
        AND type in (N'U')
)
begin
declare @createTable nvarchar(max);
set @createTable = N'
    create table ' + @tableName + '(
        [MessageId] [nvarchar](1024)not null primary key,
        [Dispatched] [bit]not null DEFAULT 0,
        [DispatchedAt] [datetime],
        [Operations] [nvarchar](max)not null
    )
';
exec(@createTable);
end
