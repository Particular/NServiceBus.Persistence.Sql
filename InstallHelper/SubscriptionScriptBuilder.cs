using System.IO;

namespace NServiceBus.SqlPersistence
{
    public static class SubscriptionScriptBuilder
    {
        public static void BuildCreate(string schema, string endpointName, TextWriter writerBuilder)
        {
            writerBuilder.Write(@"
IF NOT  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}.SubscriptionData]') AND type in (N'U'))
BEGIN
    CREATE TABLE [{0}].[{1}.SubscriptionData](
	    [Subscriber] [varchar](450) NOT NULL,
	    [MessageType] [varchar](450) NOT NULL,
        PRIMARY KEY CLUSTERED 
        (
	        [Subscriber] ASC,
	        [MessageType] ASC
        )
    )
END
", schema, endpointName);
            
        }

        public static void BuildDrop(string schema, string endpointName, TextWriter writerBuilder)
        {
            writerBuilder.Write(@"
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}.SubscriptionData]') AND type in (N'U'))
BEGIN
    DROP TABLE [{0}].[{1}.SubscriptionData]
END
", schema, endpointName);
        }
    }
}