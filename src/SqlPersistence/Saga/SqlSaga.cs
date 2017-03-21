using System;
using System.Reflection;

namespace NServiceBus.Persistence.Sql
{
    using System.Linq.Expressions;

    /// <summary>
    /// Base class for all sagas being stored by the SQL Persistence. Replaces <see cref="Saga{TSagaData}"/>.
    /// </summary>
    public abstract class SqlSaga<TSagaData> : Saga
        where TSagaData :
        IContainSagaData,
        new()
    {

        internal void VerifyNoConfigureHowToFindSaga()
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var type = GetType();
            var methodInfo = type.GetMethod("ConfigureHowToFindSaga", bindingFlags);
            if (methodInfo != null)
            {
                throw new Exception($"SqlSaga should only have ConfigureMapping(MessagePropertyMapper) overriden and not ConfigureHowToFindSaga({nameof(IConfigureHowToFindSagaWithMessage)}). Saga: {type.FullName}.");
            }
        }

        void VerifyBase()
        {
            var baseTypeFullName = GetType().BaseType.FullName;
            var fullName = typeof(SqlSaga<>).FullName;
            if (!baseTypeFullName.StartsWith(fullName))
            {
                throw new Exception($"Implementations of {fullName} must inherit from it directly. Deep class hierarchies are not supported.");
            }
        }

        /// <summary>
        /// Gets the name of the correlation property for <typeparamref name="TSagaData"/>.
        /// </summary>
        protected abstract string CorrelationPropertyName { get; }

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

        /// <summary>
        /// <see cref="Saga.ConfigureHowToFindSaga"/>. Do not override this method.
        /// </summary>
        protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage mapper)
        {
            VerifyNoConfigureHowToFindSaga();
            VerifyBase();
            var propertyMapper = new MessagePropertyMapper<TSagaData>(mapper, GetExpression(), GetType());
            ConfigureMapping(propertyMapper);
        }

        /// <summary>
        /// Allows messages to be mapped to <see cref="CorrelationPropertyName"/>.
        /// </summary>
        protected abstract void ConfigureMapping(IMessagePropertyMapper mapper);

        internal Expression<Func<TSagaData, object>> GetExpression()
        {
            var correlationProperty = GetCorrelationProperty();
            var parameterExpression = Expression.Parameter(typeof(TSagaData));
            var propertyExpression = Expression.Property(parameterExpression, correlationProperty);
            var convert = Expression.Convert(propertyExpression, typeof(object));
            return Expression.Lambda<Func<TSagaData, object>>(convert, parameterExpression);
        }

        PropertyInfo GetCorrelationProperty()
        {
            var correlationProperty = typeof(TSagaData)
                .GetProperty(CorrelationPropertyName, BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);
            if (correlationProperty != null)
            {
                return correlationProperty;
            }
            var message = $"Expected to find a property named '{CorrelationPropertyName}' on [{typeof(TSagaData).FullName}].";
            throw new Exception(message);
        }
    }
}