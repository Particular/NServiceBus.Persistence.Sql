using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class SwitchingLogicSaga : Saga<SwitchingLogicSaga.SagaData>
    {
        int number = 0;

        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string OtherProperty { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            if (number > 0)
            {
                mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            }
            else
            {
                mapper.ConfigureMapping<MessageB>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            }
        }
    }
}
