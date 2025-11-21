using System;
using System.Linq.Expressions;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;

class PropertyMapper<TSagaData>(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Expression<Func<TSagaData, object>> sagaEntityProperty, Type sagaType)
    : IMessagePropertyMapper
    where TSagaData : class, IContainSagaData
{
    public void ConfigureMapping<TMessage>(Expression<Func<TMessage, object>> messageProperty)
    {
        if (sagaEntityProperty == null)
        {
            throw new Exception($"The saga '{sagaType.FullName}' has not defined a CorrelationPropertyName, so it is expected that a {nameof(ISagaFinder<TSagaData, TMessage>)} will be defined for all messages the saga handles.");
        }
        ArgumentNullException.ThrowIfNull(messageProperty);
        sagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageProperty);
    }
}