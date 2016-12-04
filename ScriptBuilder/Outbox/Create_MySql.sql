set @tableName = '[' + @schema + '].[' + @endpointName + 'OutboxData]';

set @createTable =  concat('
    create table if not exists ', @tableName, '(
        [MessageId] [nvarchar](1024)not null primary key,
        [Dispatched] [bit]not null default 0,
        [DispatchedAt] [datetime],
        [Operations] [nvarchar](max)not null
    )
');
prepare stmt from @createTable;
execute stmt;
