SET @tableName = CONCAT('[', @schema, '].[', @endpointName, 'TimeoutData]');

SET @createTable = CONCAT('
    CREATE TABLE IF NOT EXISTS ' + @tableName + '(
        [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
        [Destination] [nvarchar](1024),
        [SagaId] [uniqueidentifier],
        [State] [varbinary](max),
        [Time] [datetime],
        [Headers] [nvarchar](max) NOT NULL,
        [PersistenceVersion] [nvarchar](23) NOT NULL
    )
');
PREPARE stmt FROM @createTable;
EXECUTE stmt;
