using NServiceBus.Saga;

namespace ReferenceLibrary
{
    public class BaseInAnotherAssemblySaga : Saga<BaseSagaData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<BaseSagaData> mapper)
        {
        }
    }
    public class GenericBaseInAnotherAssemblySaga<T> : Saga<T> where T : IContainSagaData, new()
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<T> mapper)
        {
        }
    }
}