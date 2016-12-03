using System.IO;
using System.Text;
using NServiceBus.Persistence.Sql.ScriptBuilder;

class MsSqlServerSagaScriptWriter : ISagaScriptWriter
{
    TextWriter writer;
    SagaDefinition saga;

    public MsSqlServerSagaScriptWriter(TextWriter textWriter, SagaDefinition saga)
    {
        writer = textWriter;
        this.saga = saga;
    }

    public void WriteTableNameVariable()
    {
        writer.Write(@"
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '{0}]';
", saga.TableSuffix);
    }

    public void AddProperty(CorrelationProperty correlationProperty)
    {
        var columnType = CorrelationPropertyTypeConverter.GetColumnType(correlationProperty.Type);
        writer.Write($@"
IF NOT EXISTS
(
  SELECT * FROM sys.columns
  WHERE
    name = 'Correlation_{correlationProperty.Name}' AND
    object_id = OBJECT_ID(@tableName)
)
BEGIN
  DECLARE @createColumn_{correlationProperty.Name} nvarchar(max);
  SET @createColumn_{correlationProperty.Name} = '
  ALTER TABLE ' + @tableName  + '
    ADD Correlation_{correlationProperty.Name} {columnType};
  ';
  exec(@createColumn_{correlationProperty.Name});
END
");
    }

    public void VerifyColumnType(CorrelationProperty correlationProperty)
    {
        var columnType = CorrelationPropertyTypeConverter.GetColumnType(correlationProperty.Type);
        var correlationPropertyName = correlationProperty.Name;
        writer.Write($@"
DECLARE @dataType_{correlationPropertyName} nvarchar(max);
SET @dataType_{correlationPropertyName} = (
  SELECT DATA_TYPE
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE
    TABLE_NAME = ' + @tableName  + ' AND
    COLUMN_NAME = 'Correlation_{correlationPropertyName}'
);
IF (@dataType_{correlationPropertyName} <> '{columnType}')
  THROW 50000, 'Incorrect data type for {columnType}', 0
");
    }

    public void WriteCreateIndex(CorrelationProperty correlationProperty)
    {
        writer.Write($@"
IF NOT EXISTS
(
    SELECT *
    FROM sys.indexes
    WHERE
        name = 'Index_Correlation_{correlationProperty.Name}' AND
        object_id = OBJECT_ID(@tableName)
)
BEGIN
  DECLARE @createIndex_{correlationProperty.Name} nvarchar(max);
  SET @createIndex_{correlationProperty.Name} = '
  CREATE UNIQUE NONCLUSTERED INDEX Index_Correlation_{correlationProperty.Name}
  ON ' + @tableName  + '(Correlation_{correlationProperty.Name})
  WHERE Correlation_{correlationProperty.Name} IS NOT NULL;
';
  exec(@createIndex_{correlationProperty.Name});
END
");
    }

    public void WritePurgeObsoleteProperties()
    {
        var builder = new StringBuilder();

        if (saga.CorrelationProperty != null)
        {
            builder.Append($" AND\r\n        col.COLUMN_NAME <> 'Correlation_{saga.CorrelationProperty.Name}'");
        }
        if (saga.TransitionalCorrelationProperty != null)
        {
            builder.Append($" AND\r\n        col.COLUMN_NAME <> 'Correlation_{saga.TransitionalCorrelationProperty.Name}'");
        }
        writer.Write($@"
declare @dropPropertiesQuery nvarchar(max);
select @dropPropertiesQuery =
(
    SELECT 'ALTER TABLE ' + @tableName  + ' DROP COLUMN ' + col.COLUMN_NAME '; '
    FROM INFORMATION_SCHEMA.COLUMNS col
    WHERE
        col.TABLE_NAME = ' + @tableName  + ' AND
        col.COLUMN_NAME LIKE 'Correlation_%'{builder}
);
exec sp_executesql @dropPropertiesQuery
");
    }

    public void WritePurgeObsoleteIndex()
    {
        var builder = new StringBuilder();

        if (saga.CorrelationProperty != null)
        {
            builder.Append($" AND\r\n        ix.Name <> 'Index_Correlation_{saga.CorrelationProperty.Name}'");
        }
        if (saga.TransitionalCorrelationProperty != null)
        {
            builder.Append($" AND\r\n        ix.Name <> 'Index_Correlation_{saga.TransitionalCorrelationProperty.Name}'");
        }

        writer.Write($@"
declare @dropIndexQuery nvarchar(max);
select @dropIndexQuery =
(
    SELECT 'DROP INDEX ' + ix.name + ' ON ' + @tableName + '; '
    FROM sysindexes ix
    WHERE
        ix.Id = (select object_id from sys.objects where name = @tableName) AND
        ix.Name IS NOT null AND
        ix.Name LIKE 'Index_Correlation_%'{builder}
);
exec sp_executesql @dropIndexQuery
");
    }

    public void WriteCreateTable()
    {
        writer.Write(@"
IF NOT EXISTS
(
    SELECT *
    FROM sys.objects
    WHERE
        object_id = OBJECT_ID(@tableName) AND
        type in ('U')
)
BEGIN
DECLARE @createTable nvarchar(max);
SET @createTable = '
    CREATE TABLE ' + @tableName + '(
        [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
        [Originator] [nvarchar](255),
        [OriginalMessageId] [nvarchar](255),
        [Data] [nvarchar](max) NOT NULL,
        [PersistenceVersion] [nvarchar](23) NOT NULL,
        [SagaTypeVersion] [nvarchar](23) NOT NULL
    )
';
exec(@createTable);
END
");
    }

    public void WriteDropTable()
    {
        writer.Write(@"
IF EXISTS
(
    SELECT *
    FROM sys.objects
    WHERE
        object_id = OBJECT_ID(@tableName)
        AND type in ('U')
)
BEGIN
    DECLARE @createTable nvarchar(max);
    SET @createTable = 'DROP TABLE ' + @tableName;
    exec(@createTable);
END
");
    }
}