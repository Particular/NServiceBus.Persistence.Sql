﻿[assembly: SqlServerTest]

namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public partial class TestSuiteConstraints
    {
        public bool SupportsDtc => false;
        public bool SupportsCrossQueueTransactions => true;
        public bool SupportsNativePubSub => true;
        public bool SupportsDelayedDelivery => true;
        public bool SupportsOutbox => true;
        public bool SupportsPurgeOnStartup => true;
        public IConfigureEndpointTestExecution CreateTransportConfiguration()
        {
            return new ConfigureEndpointPostgreSqlTransport();
        }

        public IConfigureEndpointTestExecution CreatePersistenceConfiguration()
        {
            return new ConfigureEndpointSqlPersistence();
        }
    }
}