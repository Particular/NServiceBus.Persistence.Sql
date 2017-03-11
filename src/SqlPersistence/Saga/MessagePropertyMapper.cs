using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// For automapping saga correlation properties by using a <see cref="CorrelatedSagaAttribute"/>. Used by <see cref="SqlSaga{TSagaData}"/>.
    /// </summary>
    public class MessagePropertyMapper<TSagaData> where TSagaData : IContainSagaData, new()
    {
        SagaPropertyMapper<TSagaData> sagaPropertyMapper;
        Expression<Func<TSagaData, object>> correlationExpression;

        internal MessagePropertyMapper(SagaPropertyMapper<TSagaData> sagaPropertyMapper, Type sagaType)
        {
            this.sagaPropertyMapper = sagaPropertyMapper;
            correlationExpression = GetExpression(sagaType);
        }

        internal static Expression<Func<TSagaData, object>>  GetExpression(Type sagaType)
        {
            var sagaDataType = typeof (TSagaData);
            if (sagaType.GetCustomAttribute<AlwaysStartNewSagaAttribute>() != null)
            {
                return null;
            }
            var sagaAttribute = sagaType.GetCustomAttribute<CorrelatedSagaAttribute>();
            if (sagaAttribute == null)
            {
                var message = $"Implementations of SqlSaga require a [{nameof(CorrelatedSagaAttribute)}] to be applied.";
                throw new Exception(message);
            }
            if (sagaAttribute.CorrelationProperty == null)
            {
                var message = $"When implementing a SqlSaga it is necessary to provide a CorrelationProperty via a [{nameof(CorrelatedSagaAttribute)}]. Either provide a CorrelationProperty or inherit from {nameof(Saga)} instead.";
                throw new Exception(message);
            }
            var correlationProperty = sagaDataType
                .GetProperty(sagaAttribute.CorrelationProperty, BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);
            if (correlationProperty == null)
            {
                var message = $"Expected to find a property named {sagaAttribute.CorrelationProperty} on  [{sagaDataType.FullName}].";
                throw new Exception(message);
            }
            var parameterExpression = Expression.Parameter(typeof (TSagaData), "s");

            var propertyExpression = Expression.Property(parameterExpression, correlationProperty);
            if (correlationProperty.PropertyType == typeof (string))
            {
                return Expression.Lambda<Func<TSagaData, object>>(propertyExpression, parameterExpression);
            }
            var convert = Expression.Convert(propertyExpression, typeof(object));
            return Expression.Lambda<Func<TSagaData, object>>(convert, parameterExpression);
        }

        /// <summary>
        /// Specify how to map between <typeparamref name="TMessage"/> and the correlation property on <typeparamref name="TSagaData"/>.
        /// </summary>
        /// <typeparam name="TMessage">The message type to map to.</typeparam>
        /// <param name="messageProperty">An <see cref="Expression"/> that represents the message property to map to.</param>
        public void MapMessage<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
            if (correlationExpression == null)
            {
                return;
            }
            var configureMapping = sagaPropertyMapper.ConfigureMapping(messageProperty);
            configureMapping.ToSaga(correlationExpression);
        }
    }
}