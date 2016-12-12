set @fullTableName = concat(@schema, '.', @tablePrefix, 'OutboxData');
set @createTable =  concat('
    create table if not exists ', @fullTableName, '(
        MessageId varchar(1024) not null,
        Dispatched bit not null default 0,
        DispatchedAt datetime,
        Operations longtext not null,
        primary key (`MessageId`)
    ) DEFAULT CHARSET=utf8;
');
prepare statment from @createTable;
execute statment;
deallocate prepare statment;