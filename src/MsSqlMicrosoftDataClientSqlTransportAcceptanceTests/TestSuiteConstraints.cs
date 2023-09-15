[assembly: SqlServerTest]

namespace NServiceBus.AcceptanceTests
{
    using System;
    using AcceptanceTesting.Support;

    public partial class TestSuiteConstraints
    {
        public bool SupportsDtc => OperatingSystem.IsWindows();
        public bool SupportsCrossQueueTransactions => true;
        public bool SupportsNativePubSub => false;
        public bool SupportsDelayedDelivery => true;
        public bool SupportsOutbox => true;
        public bool SupportsPurgeOnStartup => true;
        public IConfigureEndpointTestExecution CreateTransportConfiguration()
        {
            return new ConfigureEndpointSqlServerTransport();
        }

        public IConfigureEndpointTestExecution CreatePersistenceConfiguration()
        {
            return new ConfigureEndpointSqlPersistence();
        }
    }
}