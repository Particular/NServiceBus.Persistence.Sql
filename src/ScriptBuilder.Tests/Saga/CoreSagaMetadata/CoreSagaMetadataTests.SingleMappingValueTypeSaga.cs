using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class SingleMappingValueTypeSaga : Saga<SingleMappingValueTypeSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public int Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.Correlation).ToMessage<MessageA>(msg => msg.Correlation);
    }
}