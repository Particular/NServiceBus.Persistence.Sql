using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class HeaderMappingSaga : Saga<HeaderMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string SagaCorrelation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureHeaderMapping<MessageA>("SomeHeaderName")
                .ToSaga(saga => saga.SagaCorrelation);
        }
    }
}