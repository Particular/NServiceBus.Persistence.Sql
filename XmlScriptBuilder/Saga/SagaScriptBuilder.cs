using System;
using System.IO;

namespace NServiceBus.Persistence.Sql.Xml
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
            WritePurgeObsoleteProperties(saga,writer);
        }

        static void WriteTableNameVariable(SagaDefinition saga, TextWriter writer)
        {
            writer.Write(@"
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '{0}]';
", saga.Name);
        }

        static void AddProperty(CorrelationMember constraintMember, TextWriter writer)
        {
            if (constraintMember == null)
            {
                return;
            }
            var columnType = GetColumnType(constraintMember.Type);
            var columnName = GetColumnName(constraintMember);
            writer.Write($@"
IF NOT EXISTS
(
  SELECT * FROM sys.columns
  WHERE
    name = '{columnName}' AND
    object_id = OBJECT_ID(@tableName)
)
BEGIN
  DECLARE @createColumn{columnName} nvarchar(max);
  SET @createColumn{columnName} = '
  ALTER TABLE ' + @tableName  + '
    ADD {columnName} {columnType};
  ';
  exec(@createColumn{columnName});
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
            var columnName = GetColumnName(correlationMember);
            writer.Write($@"
DECLARE @dataType{columnName} nvarchar(max);
SET @dataType{columnName} = (
  SELECT DATA_TYPE
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE
    TABLE_NAME = ' + @tableName  + ' AND
    COLUMN_NAME = '{columnName}'
);
IF (@dataType{columnName} <> '{columnType}')
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
            var columnName = GetColumnName(correlationMember);
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
  DECLARE @createIndex{columnName} nvarchar(max);
  SET @createIndex{columnName} = '
  CREATE UNIQUE NONCLUSTERED INDEX {indexName}
  ON ' + @tableName  + '({columnName})
  WHERE {columnName} IS NOT NULL;
';
  exec(@createIndex{columnName});
END
");
        }

        const string propertyPrefix = "Property_";
        static string GetColumnName(CorrelationMember correlationMember)
        {
            return $"{propertyPrefix}{correlationMember.Name}";
        }

        static string GetIndexName(CorrelationMember correlationMember)
        {
            // Index names must be unique within a table or view but do not have to be unique within a database.
            return $"Index_{propertyPrefix}{correlationMember.Name}";
        }
        static void WritePurgeObsoleteIndexes(SagaDefinition saga, TextWriter writer)
        {
            writer.Write(@"
declare @dropIndexQuery nvarchar(max);
select @dropIndexQuery =
(
    SELECT 'DROP INDEX ' + ix.name + ' ON ' + @tableName + '; '
    FROM sysindexes ix
    WHERE
		ix.Id = (select object_id from sys.objects where name = @tableName) AND
	    ix.Name IS NOT null AND
	    ix.Name LIKE 'PropertyIndex_%' AND
	    ix.Name <> 'PropertyIndex_{0}'
    for xml path('')
);
exec sp_executesql @dropIndexQuery
", saga.CorrelationMember);
        }
        static void WritePurgeObsoleteProperties(SagaDefinition saga, TextWriter writer)
        {
            var correlationColumnName = "";
            var correlationIndexName = "";
            if (saga.CorrelationMember != null)
            {
                correlationColumnName = GetColumnName(saga.CorrelationMember);
                correlationIndexName = GetIndexName(saga.CorrelationMember);
            }
            var transitionalColumnName = "";
            var transitionalIndexName = "";
            if (saga.TransitionalCorrelationMember != null)
            {
                transitionalColumnName = GetColumnName(saga.TransitionalCorrelationMember);
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
    for xml path('')
);
exec sp_executesql @dropIndexQuery
");

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