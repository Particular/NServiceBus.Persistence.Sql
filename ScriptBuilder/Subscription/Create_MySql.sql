set @tableName = concat('[', @schema, '].[', @tablePrefix, 'SubscriptionData]');
set @createTable = concat('
    create table if not exists ', @tableName, '(
        [Subscriber] [varchar](450) not null,
        [Endpoint] [varchar](450) null,
        [MessageType] [varchar](450) not null,
        [PersistenceVersion] [nvarchar](23) not null,
        primary key clustered
        (
            [Subscriber] asc,
            [MessageType] asc
        )
    )
');
prepare stmt from @createTable;
execute stmt;