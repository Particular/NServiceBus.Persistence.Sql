using System;
using System.IO;
using System.Text;

namespace NServiceBus.Persistence.Sql
{
    public static class SagaScriptBuilder
    {

        public static void BuildCreateScript(SagaDefinition saga, TextWriter writer)
        {
            Guard.AgainstNull(nameof(saga), saga);
            Guard.AgainstNull(nameof(writer), writer);

            SagaDefinitionValidator.ValidateSagaDefinition(
                correlationProperty: saga.CorrelationProperty?.Name, 
                sagaName: saga.Name, 
                tableSuffix: saga.TableSuffix,
                transitionalProperty: saga.TransitionalCorrelationProperty?.Name);

            WriteTableNameVariable(saga, writer);
            WriteCreateTable(writer);
            if (saga.CorrelationProperty != null)
            {
                AddProperty(saga.CorrelationProperty, writer);
                VerifyColumnType(saga.CorrelationProperty, writer);
                WriteCreateIndex(saga.CorrelationProperty, writer);
            }
            if (saga.TransitionalCorrelationProperty != null)
            {
                AddProperty(saga.TransitionalCorrelationProperty, writer);
                VerifyColumnType(saga.TransitionalCorrelationProperty, writer);
                WriteCreateIndex(saga.TransitionalCorrelationProperty, writer);
            }
            WritePurgeObsoleteIndex(saga, writer);
            WritePurgeObsoleteProperties(saga, writer);
        }

        static void WriteTableNameVariable(SagaDefinition saga, TextWriter writer)
        {
            writer.Write(@"
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '{0}]';
", saga.TableSuffix);
        }

        static void AddProperty(CorrelationProperty correlationProperty, TextWriter writer)
        {
            var columnType = GetColumnType(correlationProperty.Type);
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

        static void VerifyColumnType(CorrelationProperty correlationProperty, TextWriter writer)
        {
            var columnType = GetColumnType(correlationProperty.Type);
            writer.Write($@"
DECLARE @dataType_{correlationProperty.Name} nvarchar(max);
SET @dataType_{correlationProperty.Name} = (
  SELECT DATA_TYPE
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE
    TABLE_NAME = ' + @tableName  + ' AND
    COLUMN_NAME = 'Correlation_{correlationProperty.Name}'
);
IF (@dataType_{correlationProperty.Name} <> '{columnType}')
  THROW 50000, 'Incorrect data type for {columnType}', 0
");
        }

        static string GetColumnType(CorrelationPropertyType propertyType)
        {
            switch (propertyType)
            {
                case CorrelationPropertyType.DateTime:
                    return "datetime";
                case CorrelationPropertyType.DateTimeOffset:
                    return "datetimeoffset";
                case CorrelationPropertyType.String:
                    return "nvarchar(450)";
                case CorrelationPropertyType.Int:
                    return "bigint";
                case CorrelationPropertyType.Guid:
                    return "uniqueidentifier";
            }
            throw new Exception($"Could not convert {propertyType}.");
        }

        static void WriteCreateIndex(CorrelationProperty correlationProperty, TextWriter writer)
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

        static void WritePurgeObsoleteProperties(SagaDefinition saga, TextWriter writer)
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

        static void WritePurgeObsoleteIndex(SagaDefinition saga, TextWriter writer)
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

        static void WriteCreateTable(TextWriter writer)
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
        [Data] [xml] NOT NULL,
        [PersistenceVersion] [nvarchar](23) NOT NULL,
        [SagaTypeVersion] [nvarchar](23) NOT NULL
    )
';
exec(@createTable);
END
");
        }

        public static void BuildDropScript(SagaDefinition saga, TextWriter writer)
        {
            WriteTableNameVariable(saga, writer);
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
}