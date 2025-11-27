using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class ReverseMappingSaga : Saga<ReverseMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string SagaCorrelation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(saga => saga.SagaCorrelation)
                .ToMessage<MessageA>(msg => msg.Correlation);
    }
}