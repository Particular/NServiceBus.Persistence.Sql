using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class MethodCallInMappingSaga : Saga<MethodCallInMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => MapInMethod(saga));
        }

        static object MapInMethod(SagaData data)
        {
            return data.Correlation;
        }
    }
}
