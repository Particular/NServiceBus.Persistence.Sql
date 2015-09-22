//using System.Data.SqlClient;
//using NServiceBus;
//using NServiceBus.Installation;

//class SubscriptionInstaller : INeedToInstallSomething
//{
//    string connectionString;

//    public SubscriptionInstaller(string connectionString)
//    {
//        this.connectionString = connectionString;
//    }
//    public void Install(string identity, Configure config)
//    {
//        var script = @"
//if not exists (select * from sysobjects where name='Subscription' and xtype='U')
//
//CREATE TABLE [dbo].[Subscription](
//	[SubscriberEndpoint] [varchar](450) NOT NULL,
//	[MessageType] [varchar](450) NOT NULL,
//	[Version] [varchar](450) NULL,
//	[TypeName] [varchar](450) NULL,
//PRIMARY KEY CLUSTERED 
//(
//	[SubscriberEndpoint] ASC,
//	[MessageType] ASC
//)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
//) ON [PRIMARY]
//
//GO
//";
//        using (var sqlConnection = new SqlConnection(connectionString))
//        using (var command = new SqlCommand(script, sqlConnection))
//        {
//            command.ExecuteNonQuery();
//        }
//    }
//}