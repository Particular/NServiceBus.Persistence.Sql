using System;
using System.Linq;
using System.Linq.Expressions;
using NServiceBus.Saga;

namespace NServiceBus.SqlPersistence
{
    public class MessagePropertyMapper<TSagaData> where TSagaData : XmlSagaData, new()
    {
        SagaPropertyMapper<TSagaData> sagaPropertyMapper;
        Expression<Func<TSagaData, object>> correlationExpression;

        internal MessagePropertyMapper(SagaPropertyMapper<TSagaData> sagaPropertyMapper)
        {
            this.sagaPropertyMapper = sagaPropertyMapper;
            TryGetExpression(out correlationExpression);
        }

        internal static bool TryGetExpression(out Expression<Func<TSagaData, object>> expression)
        {
            var sagaDataType = typeof (TSagaData);
            var correlationMember = sagaDataType
                .GetMembers()
                .FirstOrDefault(x => x.ContainsAttribute<CorrelationIdAttribute>());
            if (correlationMember == null)
            {
                expression = null;
                return false;
            }

            var parameterExpression = Expression.Parameter(typeof (TSagaData), "s");
            expression = Expression.Lambda<Func<TSagaData, object>>(Expression.PropertyOrField(parameterExpression, correlationMember.Name), parameterExpression);
            return true;
        }

        /// <summary>
        /// Specify how to map between <typeparamref name="TSagaData"/> and <typeparamref name="TMessage"/>.
        /// </summary>
        /// <typeparam name="TMessage">The message type to map to.</typeparam>
        /// <param name="messageProperty">An <see cref="Expression"/> that represents the message.</param>
        /// <returns>A <see cref="ToSagaExpression{TSagaData,TMessage}"/> that provides the fluent chained <see cref="ToSagaExpression{TSagaData,TMessage}.ToSaga"/> to link <paramref name="messageProperty"/> with <typeparamref name="TSagaData"/>.</returns>
        public void MapMessage<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
            if (correlationExpression == null)
            {
                var message = $"You are attempting to map a message property but no correlation property has been defined for '{typeof (TSagaData).FullName}'. Please add a [CorrelationIdAttribute] to the saga data member you wish to correlate on.";
                throw new Exception(message);
            }
            var configureMapping = sagaPropertyMapper.ConfigureMapping(messageProperty);
            configureMapping.ToSaga(correlationExpression);
        }
    }
}