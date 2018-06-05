using NServiceBus;

public partial class CoreSagaMetadataTests
{
    public class PassingMapperSaga : Saga<PassingMapperSaga.SagaData>
    {
        public class SagaData : ContainSagaData
        {
            public string Correlation { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            PassTheMapper(mapper);
        }

        static void PassTheMapper(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<MessageA>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
        }
    }
}
/* IL:
PassingMapperSaga.ConfigureHowToFindSaga:
IL_0000:  ldarg.1     
IL_0001:  call        UserQuery+PassingMapperSaga.PassTheMapper
IL_0006:  ret  
*/