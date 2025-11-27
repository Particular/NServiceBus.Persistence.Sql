using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class SwitchingLogicSaga : Saga<SwitchingLogicSaga.SagaData>
    {
        readonly int number = 0;

        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string OtherProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            if (number > 0)
            {
                mapper.MapSaga(s => s.Correlation).ToMessage<MessageA>(msg => msg.Correlation);
            }
            else
            {
                mapper.MapSaga(s => s.Correlation).ToMessage<MessageB>(msg => msg.Correlation);
            }
        }
    }
}