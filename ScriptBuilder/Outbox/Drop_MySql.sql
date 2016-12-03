SET @tableName = '[' + @schema + '].[' + @endpointName + 'OutboxData]';

IF EXISTS
(
    SELECT *
    FROM sys.objects
    WHERE
        object_id = OBJECT_ID(@tableName) AND
        type in (N'U')
)
BEGIN
SET @dropTable = N'
    DROP TABLE ' + @tableName + '
';
exec(@dropTable);
END