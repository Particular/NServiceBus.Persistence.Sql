using NServiceBus.Saga;

namespace ReferenceLibrary
{
    public class BaseSaga:Saga<BaseSagaData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<BaseSagaData> mapper)
        {
        }
    }
}