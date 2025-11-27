using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class PassingMapperSaga : Saga<PassingMapperSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => PassTheMapper(mapper);

        static void PassTheMapper(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.Correlation).ToMessage<MessageA>(msg => msg.Correlation);
    }
}