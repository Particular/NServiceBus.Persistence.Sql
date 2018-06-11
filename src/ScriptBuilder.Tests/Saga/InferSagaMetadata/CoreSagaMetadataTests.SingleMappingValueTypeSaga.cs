using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class SingleMappingValueTypeSaga : Saga<SingleMappingValueTypeSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public int Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
        }
    }
}
