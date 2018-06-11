using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class ConcatMsgPropertiesWithInterpolationSaga : Saga<ConcatMsgPropertiesWithInterpolationSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageC>(msg => $"{msg.Part1}{msg.Part2}").ToSaga(saga => saga.Correlation);
        }
    }
}
