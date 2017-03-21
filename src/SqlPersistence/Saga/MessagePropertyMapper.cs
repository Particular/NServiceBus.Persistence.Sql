using System;
using System.Linq.Expressions;

namespace NServiceBus.Persistence.Sql
{
    using Sagas;

    class MessagePropertyMapper<TSagaData> : IMessagePropertyMapper
        where TSagaData : IContainSagaData, new()
    {
        IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
        Expression<Func<TSagaData, object>> sagaEntityProperty;
        Type sagaType;

        internal MessagePropertyMapper(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Expression<Func<TSagaData, object>> sagaEntityProperty, Type sagaType)
        {
            this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
            this.sagaEntityProperty = sagaEntityProperty;
            this.sagaType = sagaType;
        }

        public void MapMessage<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
            if (sagaEntityProperty == null)
            {
                throw new Exception($"The saga '{sagaType.FullName}' has not defined a CorrelationPropertyName, so it is expected that a {nameof(IFindSagas<TSagaData>)} will be defined for all messages the saga handles.");
            }
            Guard.AgainstNull(nameof(messageProperty), messageProperty);
            sagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageProperty);
        }
    }
}