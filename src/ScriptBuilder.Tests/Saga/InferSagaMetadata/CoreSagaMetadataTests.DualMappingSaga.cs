using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class DualMappingSaga : Saga<DualMappingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            mapper.ConfigureMapping<MessageB>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
        }
    }
}
