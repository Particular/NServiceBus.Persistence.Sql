namespace NServiceBus.SqlPersistence
{
    public static class SubscriptionScritpBuilder
    {
        public static string Build(string schema)
        {
            return string.Format(@"
IF NOT  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[Subscription]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Subscription](
	    [SubscriberEndpoint] [varchar](450) NOT NULL,
	    [MessageType] [varchar](450) NOT NULL,
	    [Version] [varchar](450) NULL,
	    [TypeName] [varchar](450) NULL,
        PRIMARY KEY CLUSTERED 
        (
	        [SubscriberEndpoint] ASC,
	        [MessageType] ASC
        )
    )
END
",schema);
            
        }
    }
}