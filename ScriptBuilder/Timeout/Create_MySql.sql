set @tableName = concat('[', @schema, '].[', @endpointName, 'TimeoutData]');

set @createTable = concat('
    create table if not exists ' + @tableName + '(
        [Id] [uniqueidentifier]not null primary key,
        [Destination] [nvarchar](1024),
        [SagaId] [uniqueidentifier],
        [State] [varbinary](max),
        [Time] [datetime],
        [Headers] [nvarchar](max) not null,
        [PersistenceVersion] [nvarchar](23)not null
    )
');
prepare stmt from @createTable;
execute stmt;
