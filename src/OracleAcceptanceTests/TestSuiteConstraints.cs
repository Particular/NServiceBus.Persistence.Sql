namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public partial class TestSuiteConstraints
    {
        public bool SupportsDtc => false;
        public bool SupportsCrossQueueTransactions => true;
        public bool SupportsNativePubSub => false;
        public bool SupportsDelayedDelivery => true;
        public bool SupportsOutbox => true;

        public IConfigureEndpointTestExecution CreateTransportConfiguration()
        {
            return new ConfigureEndpointAcceptanceTestingTransport(SupportsNativePubSub, SupportsDelayedDelivery);
        }

        public IConfigureEndpointTestExecution CreatePersistenceConfiguration()
        {
            return new ConfigureEndpointSqlPersistence();
        }
    }
}