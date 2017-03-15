using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// For automapping saga correlation properties by using a <see cref="SqlSagaAttribute"/>. Used by <see cref="SqlSaga{TSagaData}"/>.
    /// </summary>
    public sealed class MessagePropertyMapper<TSagaData>
        where TSagaData : IContainSagaData, new()
    {
        IConfigureHowToFindSagaWithMessage findingConfiguration;
        Expression<Func<TSagaData, object>> correlationExpression;

        internal MessagePropertyMapper(IConfigureHowToFindSagaWithMessage findingConfiguration, Type sagaType)
        {
            this.findingConfiguration = findingConfiguration;
            correlationExpression = GetExpression(sagaType);
        }

        internal static Expression<Func<TSagaData, object>> GetExpression(Type sagaType)
        {
            var sagaDataType = typeof (TSagaData);
            var sqlSagaAttribute = sagaType.GetCustomAttribute<SqlSagaAttribute>();
            if (sqlSagaAttribute == null)
            {
                var message = $"Implementations of SqlSaga require a [{nameof(SqlSagaAttribute)}] to be applied.";
                throw new Exception(message);
            }
            if (sqlSagaAttribute.CorrelationProperty == null)
            {
                return null;
            }
            var correlationProperty = sagaDataType
                .GetProperty(sqlSagaAttribute.CorrelationProperty, BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);
            if (correlationProperty == null)
            {
                var message = $"Expected to find a property named {sqlSagaAttribute.CorrelationProperty} on  [{sagaDataType.FullName}].";
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
            Guard.AgainstNull(nameof(messageProperty), messageProperty);
            if (correlationExpression == null)
            {
                var message = $"Attempting to map a message property but no correlation property has been defined for '{typeof (TSagaData).FullName}'. Add a [{nameof(SqlSagaAttribute)}] to the saga.";
                throw new Exception(message);
            }
            findingConfiguration.ConfigureMapping(correlationExpression, messageProperty);
        }
    }
}