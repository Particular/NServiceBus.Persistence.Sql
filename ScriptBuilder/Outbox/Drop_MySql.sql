set @fullTableName = concat(@schema, '.', @tablePrefix, 'OutboxData');
set @dropTable = concat('drop table if exists ', @fullTableName);
prepare statment from @dropTable;
execute statment;	   
deallocate prepare statment;