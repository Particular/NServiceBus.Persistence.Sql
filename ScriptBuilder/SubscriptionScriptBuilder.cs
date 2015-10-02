using System.IO;

namespace NServiceBus.SqlPersistence
{
    public static class SubscriptionScriptBuilder
    {
        public static void BuildCreateScript(string schema, string endpointName, TextWriter writer)
        {
            writer.Write(@"
declare @schema nvarchar(max) = '{0}';
declare @endpointName nvarchar(max) = '{1}';
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '.SubscriptionData]';
", schema, endpointName);
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
    CREATE TABLE ' + @tableName + '(
	    [Subscriber] [varchar](450) NOT NULL,
	    [MessageType] [varchar](450) NOT NULL,
	    [PersistenceVersion] [nvarchar](23) NOT NULL,
        PRIMARY KEY CLUSTERED 
        (
	        [Subscriber] ASC,
	        [MessageType] ASC
        )
    )
';
exec(@createTable);
END
");
        }

        public static void BuildDropScript(string schema, string endpointName, TextWriter writer)
        {
            writer.Write(@"
declare @schema nvarchar(max) = '{0}';
declare @endpointName nvarchar(max) = '{1}';
declare @tableName nvarchar(max) = '[' + @schema + '].[' + @endpointName + '.SubscriptionData]';
", schema, endpointName);
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