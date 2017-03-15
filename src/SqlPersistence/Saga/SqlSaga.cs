using System;
using System.Reflection;

namespace NServiceBus.Persistence.Sql
{
    public abstract class SqlSaga<TSagaData> : Saga
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
                throw new Exception($"SqlSaga should only have ConfigureMapping(MessagePropertyMapper) overriden and not ConfigureHowToFindSaga({nameof(IConfigureHowToFindSagaWithMessage)}). Saga: {type.FullName}.");
            }
            verified = true;
        }

        /// <summary>
        /// The saga's strongly typed data. Wraps <see cref="Saga.Entity" />.
        /// </summary>
        public TSagaData Data
        {
            get { return (TSagaData)Entity; }
            set
            {
                Guard.AgainstNull(nameof(value), value);
                Entity = value;
            }
        }

        protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage mapper)
        {
            var messagePropertyMapper = new MessagePropertyMapper<TSagaData>(mapper, GetType());
            ConfigureMapping(messagePropertyMapper);
        }

        protected abstract void ConfigureMapping(MessagePropertyMapper<TSagaData> mapper);

    }
}