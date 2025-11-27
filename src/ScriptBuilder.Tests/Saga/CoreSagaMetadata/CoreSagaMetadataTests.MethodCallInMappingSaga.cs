using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class MethodCallInMappingSaga : Saga<MethodCallInMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => MapInMethod(s)).ToMessage<MessageA>(msg => msg.Correlation);

        static object MapInMethod(SagaData data) => data.Correlation;
    }
}