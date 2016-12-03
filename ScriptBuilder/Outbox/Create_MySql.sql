SET @tableName = '[' + @schema + '].[' + @endpointName + 'OutboxData]';

SET @createTable =  CONCAT('
    CREATE TABLE IF NOT EXISTS ', @tableName, '(
        [MessageId] [nvarchar](1024) NOT NULL PRIMARY KEY,
        [Dispatched] [bit] NOT NULL DEFAULT 0,
        [DispatchedAt] [datetime],
        [Operations] [nvarchar](max) NOT NULL
    )
');
PREPARE stmt FROM @createTable;
EXECUTE stmt;