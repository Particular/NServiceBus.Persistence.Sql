using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class WhileLoopSaga : Saga<WhileLoopSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            var i = 0;
            while (i < 3)
            {
                mapper.MapSaga(s => s.Correlation).ToMessage<MessageA>(msg => msg.Correlation);
                i++;
            }
        }
    }
}