
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + 'SubscriptionData]';
IF EXISTS 
(
    SELECT * 
    FROM sys.objects 
    WHERE 
        object_id = OBJECT_ID(@tableName) AND 
        type in (N'U')
)
BEGIN
DECLARE @dropTable nvarchar(max);
SET @dropTable = N'
    DROP TABLE ' + @tableName + '
';
exec(@dropTable);
END