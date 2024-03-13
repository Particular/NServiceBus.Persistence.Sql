using System;
using System.Linq.Expressions;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;

class PropertyMapper<TSagaData> : IMessagePropertyMapper
    where TSagaData : IContainSagaData, new()
{
    IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
    Expression<Func<TSagaData, object>> sagaEntityProperty;
    Type sagaType;

    internal PropertyMapper(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Expression<Func<TSagaData, object>> sagaEntityProperty, Type sagaType)
    {
        this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
        this.sagaEntityProperty = sagaEntityProperty;
        this.sagaType = sagaType;
    }

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