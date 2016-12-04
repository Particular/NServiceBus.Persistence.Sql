SET  @tableName = '[' + @schema + '].[' + @tablePrefix + 'SubscriptionData]';
if exists
(
    select * 
    from sys.objects 
    where 
        object_id = object_id(@tableName) and 
        type in (N'U')
)
begin
set @dropTable = N'
    drop table ' + @tableName + '
';
exec(@dropTable);
end
