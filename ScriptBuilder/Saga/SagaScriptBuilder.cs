using System;
using System.IO;

namespace NServiceBus.Persistence.Sql
{
    public static class SagaScriptBuilder
    {

        public static void BuildCreateScript(SagaDefinition saga, TextWriter writer)
        {
            WriteTableNameVariable(saga, writer);
            WriteCreateTable(writer);
            AddProperty(saga.CorrelationMember, writer);
            AddProperty(saga.TransitionalCorrelationMember, writer);
            VerifyColumnType(saga.CorrelationMember, writer);
            VerifyColumnType(saga.TransitionalCorrelationMember, writer);
            WriteCreateIndex(saga.CorrelationMember, writer);
            WriteCreateIndex(saga.TransitionalCorrelationMember, writer);
            WritePurgeObsoleteIndex(saga, writer);
            WritePurgeObsoleteProperties(saga, writer);
        }

        static void WriteTableNameVariable(SagaDefinition saga, TextWriter writer)
        {
            writer.Write(@"
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '{0}]';
", saga.Name);
        }

        static void AddProperty(CorrelationMember correlationMember, TextWriter writer)
        {
            if (correlationMember == null)
            {
                return;
            }
            var columnType = GetColumnType(correlationMember.Type);
            writer.Write($@"
IF NOT EXISTS
(
  SELECT * FROM sys.columns
  WHERE
    name = 'Correlation_{correlationMember.Name}' AND
    object_id = OBJECT_ID(@tableName)
)
BEGIN
  DECLARE @createColumn_{correlationMember.Name} nvarchar(max);
  SET @createColumn_{correlationMember.Name} = '
  ALTER TABLE ' + @tableName  + '
    ADD Correlation_{correlationMember.Name} {columnType};
  ';
  exec(@createColumn_{correlationMember.Name});
END
");
        }

        static void VerifyColumnType(CorrelationMember correlationMember, TextWriter writer)
        {
            if (correlationMember == null)
            {
                return;
            }
            var columnType = GetColumnType(correlationMember.Type);
            writer.Write($@"
DECLARE @dataType_{correlationMember.Name} nvarchar(max);
SET @dataType_{correlationMember.Name} = (
  SELECT DATA_TYPE
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE
    TABLE_NAME = ' + @tableName  + ' AND
    COLUMN_NAME = 'Correlation_{correlationMember.Name}'
);
IF (@dataType_{correlationMember.Name} <> '{columnType}')
  THROW 50000, 'Incorrect data type for {columnType}', 0
");
        }

        static string GetColumnType(CorrelationMemberType memberType)
        {
            switch (memberType)
            {
                case CorrelationMemberType.DateTime:
                    return "datetime";
                case CorrelationMemberType.DateTimeOffset:
                    return "datetimeoffset";
                case CorrelationMemberType.String:
                    return "nvarchar(450)";
                case CorrelationMemberType.Int:
                    return "bigint";
                case CorrelationMemberType.Guid:
                    return "uniqueidentifier";
            }
            throw new Exception($"Could not convert {memberType}.");
        }

        static void WriteCreateIndex(CorrelationMember correlationMember, TextWriter writer)
        {
            if (correlationMember == null)
            {
                return;
            }
            var indexName = GetIndexName(correlationMember);
            writer.Write($@"
IF NOT EXISTS
(
    SELECT *
    FROM sys.indexes
    WHERE
        name = '{indexName}' AND
        object_id = OBJECT_ID(@tableName)
)
BEGIN
  DECLARE @createIndex_{correlationMember.Name} nvarchar(max);
  SET @createIndex_{correlationMember.Name} = '
  CREATE UNIQUE NONCLUSTERED INDEX {indexName}
  ON ' + @tableName  + '(Correlation_{correlationMember.Name})
  WHERE Correlation_{correlationMember.Name} IS NOT NULL;
';
  exec(@createIndex_{correlationMember.Name});
END
");
        }

        const string propertyPrefix = "Property_";

        static string GetIndexName(CorrelationMember correlationMember)
        {
            // Index names must be unique within a table or view but do not have to be unique within a database.
            return $"Index_{propertyPrefix}{correlationMember.Name}";
        }

        static void WritePurgeObsoleteProperties(SagaDefinition saga, TextWriter writer)
        {
            var correlationColumnName = $"Correlation_{saga.CorrelationMember.Name}";
            var transitionalColumnName = "";
            if (saga.TransitionalCorrelationMember != null)
            {
                transitionalColumnName = $"Correlation_{saga.TransitionalCorrelationMember.Name}";
            }
            
            writer.Write($@"
declare @dropPropertiesQuery nvarchar(max);
select @dropPropertiesQuery =
(
    SELECT 'ALTER TABLE ' + @tableName  + ' DROP COLUMN ' + col.COLUMN_NAME '; '
    FROM INFORMATION_SCHEMA.COLUMNS col
    WHERE
        col.TABLE_NAME = ' + @tableName  + ' AND
        col.COLUMN_NAME LIKE '{propertyPrefix}%' AND
        col.COLUMN_NAME <> '{correlationColumnName}' AND
        col.COLUMN_NAME <> '{transitionalColumnName}'
);
exec sp_executesql @dropPropertiesQuery
");
        }
        static void WritePurgeObsoleteIndex(SagaDefinition saga, TextWriter writer)
        {
            var correlationIndexName = GetIndexName(saga.CorrelationMember);
            var transitionalIndexName = "";
            if (saga.TransitionalCorrelationMember != null)
            {
                transitionalIndexName = GetIndexName(saga.TransitionalCorrelationMember);
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
        ix.Name LIKE 'PropertyIndex_%' AND
        ix.Name <> 'PropertyIndex_{correlationIndexName}' AND
        ix.Name <> 'PropertyIndex_{transitionalIndexName}'
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