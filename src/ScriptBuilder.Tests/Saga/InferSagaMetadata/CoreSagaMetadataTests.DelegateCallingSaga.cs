using System;
using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class DelegateCallingSaga : Saga<DelegateCallingSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            Action action = () => mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            action();
        }
    }
}
