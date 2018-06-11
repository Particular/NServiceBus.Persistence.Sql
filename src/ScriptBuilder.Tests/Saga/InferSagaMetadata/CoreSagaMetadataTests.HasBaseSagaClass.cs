using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class HasBaseSagaClass : BaseSaga<HasBaseSagaClass.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            base.ConfigureHowToFindSaga(mapper);
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
        }
    }

    public class BaseSaga<TSaga> : Saga<TSaga>
        where TSaga : class, IContainSagaData, new()
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TSaga> mapper)
        {

        }
    }
}
