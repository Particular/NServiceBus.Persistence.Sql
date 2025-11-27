using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class SingleMappingSaga : Saga<SingleMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.Correlation).ToMessage<MessageA>(msg => msg.Correlation);
    }
}