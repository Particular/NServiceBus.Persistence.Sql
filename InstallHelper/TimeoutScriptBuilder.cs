using System.IO;

namespace NServiceBus.SqlPersistence
{
    public static class TimeoutScriptBuilder
    {

        public static void BuildCreate(string schema, string endpointName, TextWriter writerBuilder)
        {
            writerBuilder.Write(@"
IF NOT  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}.TimeoutData]') AND type in (N'U'))
BEGIN
    CREATE TABLE [{0}].[{1}.TimeoutData](
	    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
	    [Destination] [nvarchar](1024) NULL,
	    [SagaId] [uniqueidentifier] NULL,
	    [State] [varbinary](max) NULL,
	    [Time] [datetime] NULL,
	    [Headers] [xml] NULL
    )
END
", schema, endpointName);
        }

        public static void BuildDrop(string schema, string endpointName, TextWriter writerBuilder)
        {
            writerBuilder.Write(@"
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}.TimeoutData]') AND type in (N'U'))
BEGIN
    DROP TABLE [{0}].[{1}.TimeoutData]
END
", schema, endpointName);
        }
    }
}