using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class ForLoopSaga : Saga<ForLoopSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            for (var i = 0; i < 3; i++)
            {
                mapper.MapSaga(s => s.Correlation).ToMessage<MessageA>(msg => msg.Correlation);
            }
        }
    }
}