﻿declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + 'OutboxData]';

if exists
(
    select *
    from sys.objects
    where
        object_id = object_id(@tableName) and
        type in (N'U')
)
begin
declare @dropTable nvarchar(max);
set @dropTable = N'
    drop table ' + @tableName + '
';
exec(@dropTable);
end
