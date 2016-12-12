set @tableName = concat(@tablePrefix, 'SubscriptionData');
set @createTable = concat('
    create table if not exists ', @tableName, '(
        Subscriber varchar(450) not null,
        Endpoint varchar(450) null,
        MessageType varchar(450) not null,
        PersistenceVersion nvarchar(23) not null,
        primary key clustered (Subscriber, MessageType)
    ) DEFAULT CHARSET=utf8;
');
prepare statment from @createTable;
execute statment;
deallocate prepare statment;