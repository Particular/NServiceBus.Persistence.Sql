using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class ConcatMsgPropertiesWithOtherMethods : Saga<ConcatMsgPropertiesWithOtherMethods.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(c => c.Correlation).ToMessage<MessageC>(msg => msg.Part1.ToUpper() + msg.Part2.ToLowerInvariant());
    }
}