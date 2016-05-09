using System;
using System.IO;

namespace NServiceBus.Persistence.SqlServerXml
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
            WriteCreateConstraint(saga.CorrelationMember,saga.Name, writer);
            WriteCreateConstraint(saga.TransitionalCorrelationMember, saga.Name, writer);
            WritePurgeObsoleteProperties(saga,writer);
        }

        static void WriteTableNameVariable(SagaDefinition saga, TextWriter writer)
        {
            writer.Write(@"
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '.{0}]';
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
  SET @createColumn{columnName} = N'
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

        static void WriteCreateConstraint(CorrelationMember correlationMember, string sagaName, TextWriter writer)
        {
            if (correlationMember == null)
            {
                return;
            }
            var constraintName = GetConstraintName(correlationMember, sagaName);
            var columnName = GetColumnName(correlationMember);
            writer.Write($@"
IF NOT EXISTS
(
  SELECT *
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_NAME = '{constraintName}'
)
BEGIN
  DECLARE @createConstraint{columnName} nvarchar(max);
  SET @createConstraint{columnName} = '
  ALTER TABLE ' + @tableName  + '
    ADD CONSTRAINT {constraintName} UNIQUE ({columnName});
  ';
  exec(@createConstraint{columnName});
END
");
        }

        const string propertyPrefix = "Property_";
        static string GetColumnName(CorrelationMember constraintMember)
        {
            return $"{propertyPrefix}{constraintMember.Name}";
        }

        static string GetConstraintName(CorrelationMember constraintMember, string sagaName)
        {
            return $"CONSTRAINT_{sagaName}_{propertyPrefix}{constraintMember.Name}";
        }

        static void WritePurgeObsoleteProperties(SagaDefinition saga, TextWriter writer)
        {
            var correlationColumnName = "";
            if (saga.CorrelationMember != null)
            {
                correlationColumnName = GetColumnName(saga.CorrelationMember);
            }
            var transitionalColumnName = "";
            if (saga.TransitionalCorrelationMember != null)
            {
                transitionalColumnName = GetColumnName(saga.TransitionalCorrelationMember);
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
        static void WriteCreateTable(TextWriter writer)
        {
            writer.Write(@"
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
    CREATE TABLE ' + @tableName + N'(
	    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
	    [Originator] [nvarchar](255) NULL,
	    [OriginalMessageId] [nvarchar](255) NULL,
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
        AND type in (N'U')
)
BEGIN
    DECLARE @createTable nvarchar(max);
    SET @createTable = N'DROP TABLE ' + @tableName + '';
    exec(@createTable);
END
");
        }
    }
}