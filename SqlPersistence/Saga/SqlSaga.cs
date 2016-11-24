
namespace NServiceBus.Persistence.Sql
{
    public abstract class SqlSaga<TSagaData> : Saga<TSagaData>
        where TSagaData :
        IContainSagaData,
        new()
    {

        //TODO: throw in MSBuild if this is overridden in child class
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TSagaData> mapper)
        {
            var messagePropertyMapper = new MessagePropertyMapper<TSagaData>(mapper, GetType());
            ConfigureMapping(messagePropertyMapper);
        }

        protected virtual void ConfigureMapping(MessagePropertyMapper<TSagaData> mapper)
        {
        }

    }
}