
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '.SubscriptionData]';

IF NOT EXISTS 
(
    SELECT * 
    FROM sys.objects 
    WHERE 
        object_id = OBJECT_ID(@tableName) AND 
        type in (N'U')
)
BEGIN
DECLARE @createTable nvarchar(max);
SET @createTable = N'
    CREATE TABLE ' + @tableName + '(
	    [Subscriber] [varchar](450) NOT NULL,
	    [MessageType] [varchar](450) NOT NULL,
	    [PersistenceVersion] [nvarchar](23) NOT NULL,
        PRIMARY KEY CLUSTERED 
        (
	        [Subscriber] ASC,
	        [MessageType] ASC
        )
    )
';
exec(@createTable);
END
