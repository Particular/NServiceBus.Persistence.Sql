using System.IO;

namespace NServiceBus.SqlPersistence
{
    public static class TimeoutScriptBuilder
    {

        public static void BuildCreateScript(TextWriter writer)
        {
            writer.Write(@"
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '.TimeoutData]';
");
            writer.Write(@"
IF NOT EXISTS (
    SELECT * FROM sys.objects 
    WHERE 
        object_id = OBJECT_ID(@tableName) 
        AND type in (N'U')
)
BEGIN
DECLARE @createTable nvarchar(max);
SET @createTable = N'
    CREATE TABLE ' + @tableName + '(
	    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
	    [Destination] [nvarchar](1024) NULL,
	    [SagaId] [uniqueidentifier] NULL,
	    [State] [varbinary](max) NULL,
	    [Time] [datetime] NULL,
	    [Headers] [xml] NULL,
	    [PersistenceVersion] [nvarchar](23) NOT NULL
    )
';
exec(@createTable);
END
");
        }

        public static void BuildDropScript(TextWriter writer)
        {
            writer.Write(@"
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '.TimeoutData]';
");
            writer.Write(@"
IF EXISTS 
(
    SELECT * 
    FROM sys.objects 
    WHERE 
        object_id = OBJECT_ID(@tableName) AND 
        type in (N'U')
)
BEGIN
DECLARE @dropTable nvarchar(max);
SET @dropTable = N'
    DROP TABLE ' + @tableName + '
';
exec(@dropTable);
END
");
        }
    }
}