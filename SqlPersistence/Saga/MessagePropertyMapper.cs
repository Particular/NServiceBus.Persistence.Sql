using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// For automapping saga correlation propertiess by using a <see cref="SqlSagaAttribute"/>. Used by <see cref="SqlSaga{TSagaData}"/>.
    /// </summary>
    public class MessagePropertyMapper<TSagaData> where TSagaData : IContainSagaData, new()
    {
        SagaPropertyMapper<TSagaData> sagaPropertyMapper;
        Expression<Func<TSagaData, object>> correlationExpression;

        internal MessagePropertyMapper(SagaPropertyMapper<TSagaData> sagaPropertyMapper, Type sagaType)
        {
            this.sagaPropertyMapper = sagaPropertyMapper;
            TryGetExpression(sagaType, out correlationExpression);
        }

        internal static bool TryGetExpression(Type sagaType, out Expression<Func<TSagaData, object>> expression)
        {
            var sagaDataType = typeof (TSagaData);
            var sqlSagaAttribute = sagaType.GetCustomAttribute<SqlSagaAttribute>();
            if (sqlSagaAttribute == null)
            {
                expression = null;
                return false;
            }
            var correlationProperty = sagaDataType
                .GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(x => x.Name == sqlSagaAttribute.CorrelationId);
            if (correlationProperty == null)
            {
                expression = null;
                return false;
            }
            var parameterExpression = Expression.Parameter(typeof (TSagaData), "s");

            var propertyExpression = Expression.Property(parameterExpression, correlationProperty);
            if (correlationProperty.PropertyType == typeof (string))
            {
                expression = Expression.Lambda<Func<TSagaData, object>>(propertyExpression, parameterExpression);
                return true;
            }
            var convert = Expression.Convert(propertyExpression, typeof(object));
            expression = Expression.Lambda<Func<TSagaData, object>>(convert, parameterExpression);
            return true;
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
                var message = $"You are attempting to map a message property but no correlation property has been defined for '{typeof (TSagaData).FullName}'. Add a [{nameof(SqlSagaAttribute)}] to the saga.";
                throw new Exception(message);
            }
            var configureMapping = sagaPropertyMapper.ConfigureMapping(messageProperty);
            configureMapping.ToSaga(correlationExpression);
        }
    }
}