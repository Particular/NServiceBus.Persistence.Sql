using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class ReverseHeaderMappingSaga : Saga<ReverseHeaderMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string SagaCorrelation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(saga => saga.SagaCorrelation)
                .ToMessageHeader<MessageA>("SomeHeaderName");
    }
}