using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class ConcatMsgPropertiesWithFormatSaga : Saga<ConcatMsgPropertiesWithFormatSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageC>(msg => string.Format("{0}{1}", msg.Part1, msg.Part2)).ToSaga(saga => saga.Correlation);
        }
    }
}
