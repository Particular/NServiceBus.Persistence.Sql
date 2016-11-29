using System;
using System.Reflection;

namespace NServiceBus.Persistence.Sql
{
    public abstract class SqlSaga<TSagaData> : Saga<TSagaData>
        where TSagaData :
        IContainSagaData,
        new()
    {
        static bool verified;

        protected SqlSaga()
        {
            if (verified)
            {
                return;
            }
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var type = GetType();
            var methodInfo = type.GetMethod("ConfigureHowToFindSaga", bindingFlags);
            if (methodInfo != null)
            {
                throw new Exception($"SqlSaga should only have ConfigureMapping(MessagePropertyMapper) overriden and not ConfigureHowToFindSaga(SagaPropertyMapper). Saga: {type.FullName}.");
            }
            verified = true;
        }

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