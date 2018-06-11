using NServiceBus;
using NServiceBus.Persistence.Sql;

public partial class CoreSagaMetadataTests
{
    [SqlSaga(transitionalCorrelationProperty: "TransitionalCorrId", tableSuffix: "DifferentTableSuffix")]
    public class MetadataInAttributeSaga : Saga<MetadataInAttributeSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
            public string TransitionalCorrId { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
        }
    }
}
