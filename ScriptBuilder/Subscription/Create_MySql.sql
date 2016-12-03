SET @tableName = CONCAT('[', @schema, '].[', @endpointName, 'SubscriptionData]');
SET @createTable =  CONCAT('
    CREATE TABLE IF NOT EXISTS ', @tableName, '(
        [Subscriber] [varchar](450) NOT NULL,
        [Endpoint] [varchar](450) NULL,
        [MessageType] [varchar](450) NOT NULL,
        [PersistenceVersion] [nvarchar](23) NOT NULL,
        PRIMARY KEY CLUSTERED
        (
            [Subscriber] ASC,
            [MessageType] ASC
        )
    )
');
PREPARE stmt FROM @createTable;
EXECUTE stmt;