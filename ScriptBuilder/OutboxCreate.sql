
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '.OutboxData]';

IF NOT EXISTS (
    SELECT * FROM sys.objects 
    WHERE 
        object_id = OBJECT_ID(@tableName) 
        AND type in (N'U')
)
BEGIN
DECLARE @createTable nvarchar(max);
SET @createTable = N'
    CREATE TABLE ' + @tableName + '(
	    [MessageId] [nvarchar](1024) NOT NULL PRIMARY KEY,
	    [Dispatched] [bit] NOT NULL DEFAULT 0,
	    [DispatchedAt] [datetime] NULL,
	    [Operations] [xml] NOT NULL,
    )
';
exec(@createTable);
END
