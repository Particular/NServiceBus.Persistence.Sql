using System;
using System.Collections.Generic;
using System.IO;

namespace NServiceBus.SqlPersistence
{
    public static class SagaScriptBuilder
    {

        public static void BuildCreateScript(string schema, string endpointName, IEnumerable<SagaDefinition> sagas, Func<string, TextWriter> writerBuilder)
        {
            foreach (var saga in sagas)
            {
                var writer = writerBuilder(saga.Name);
                WriteSaga(schema, endpointName, saga, writer);
            }
        }

        static void WriteSaga(string schema, string endpointName, SagaDefinition saga, TextWriter writer)
        {
            var tableName = $"[{schema}].[{endpointName}.{saga.Name}]";

            writer.Write(@"
declare @schema nvarchar(max) = '{0}';
declare @endpointName nvarchar(max) = '{1}';
declare @sagaName nvarchar(max) = '{2}';
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '.' + @sagaName + ']';
", schema,endpointName,saga.Name);

            WriteCreateTable(writer, tableName);

            WritePurgeObsoleteIndexes(saga, writer);

            WriteCreateIndexes(saga, writer, tableName);
        }

        static void WriteCreateIndexes(SagaDefinition saga, TextWriter writer, string tableName)
        {
            foreach (var mappedProperty in saga.MappedProperties)
            {
                writer.Write(@"
IF NOT EXISTS
(
    SELECT * 
    FROM sys.indexes 
    WHERE 
        name = 'PropertyIndex_{0}' AND 
        object_id = OBJECT_ID('{1}')
)
BEGIN
    CREATE  SELECTIVE XML INDEX PropertyIndex_{0}
    ON {1}(Data)
    FOR 
    (
        {0} = '/Data/{0}' AS XQUERY 'xs:string' SINGLETON
    )
END
", mappedProperty, tableName);
            }
        }

        static void WritePurgeObsoleteIndexes(SagaDefinition saga, TextWriter writer)
        {
            var propertyInString = "'" + string.Join("', '", saga.MappedProperties) + "'";
            writer.Write(@"
declare @dropIndexQuery nvarchar(max);
select @dropIndexQuery = 
(
    SELECT 'DROP INDEX ' + ix.name + ' ON ' + @tableName + '; '
    FROM sysindexes ix
    WHERE 
	    ix.Name IS NOT null AND 
	    ix.Name LIKE 'PropertyIndex_%' AND
	    ix.Name NOT IN ({0})
    for xml path('')
);
exec sp_executesql @dropIndexQuery
", propertyInString);
        }

        static void WriteCreateTable(TextWriter writer, string tableName)
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
DECLARE @DynamicSQL nvarchar(max);
SET @DynamicSQL = N'
    CREATE TABLE ' + @tableName + N'(
	    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
	    [Originator] [nvarchar](255) NULL,
	    [OriginalMessageId] [nvarchar](255) NULL,
	    [Data] [xml] NOT NULL,
	    [PersistenceVersion] [nvarchar](23) NOT NULL,
	    [SagaTypeVersion] [nvarchar](23) NOT NULL
    )
';
exec(@DynamicSQL);
END
", tableName);
        }

        public static void BuildDropScript(string schema, string endpointName, IEnumerable<string> sagaNames, Func<string, TextWriter> writerBuilder)
        {
            foreach (var saga in sagaNames)
            {
                var writer = writerBuilder(saga);
                writer.Write(@"
IF EXISTS 
(
    SELECT * 
    FROM sys.objects 
    WHERE 
        object_id = OBJECT_ID(N'[{0}].[{1}.{2}]') 
        AND type in (N'U')
)
BEGIN
    DROP TABLE [{0}].[{1}.{2}]
END
", schema, endpointName, saga);
            }
        }

    }
}