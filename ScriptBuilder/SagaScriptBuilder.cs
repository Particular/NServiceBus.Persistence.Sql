using System.IO;

namespace NServiceBus.SqlPersistence
{
    public static class SagaScriptBuilder
    {

        public static void BuildCreateScript(SagaDefinition saga, TextWriter writer)
        {
            writer.Write(@"
declare @sagaName nvarchar(max) = '{0}';
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '.' + @sagaName + ']';
",saga.Name);

            WriteCreateTable(writer);
            WritePurgeObsoleteIndexes(saga, writer);
            WriteCreateIndex(saga, writer);
        }

        static void WriteCreateIndex(SagaDefinition saga, TextWriter writer)
        {
            if (saga.CorrelationMember == null)
            {
                return;
            }
            writer.Write(@"
IF NOT EXISTS
(
    SELECT * 
    FROM sys.indexes 
    WHERE 
        name = 'PropertyIndex_{0}' AND 
        object_id = OBJECT_ID(@tableName)
)
BEGIN
DECLARE @createIndex nvarchar(max);
SET @createIndex = N'
    CREATE  SELECTIVE XML INDEX PropertyIndex_{0}
    ON ' + @tableName + '(Data)
    FOR 
    (
        {0} = ''/Data/{0}'' AS XQUERY ''xs:string'' SINGLETON
    )
';
exec(@createIndex);
END
", saga.CorrelationMember);
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

        
        public static void BuildDropScript(string saga, TextWriter writer)
        {
            writer.Write(@"
declare @sagaName nvarchar(max) = '{0}';
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '.' + @sagaName + ']';
", saga);
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
SET @createTable = N'
    DROP TABLE ' + @tableName + '
';
exec(@createTable);
END
");
        }
    }
}