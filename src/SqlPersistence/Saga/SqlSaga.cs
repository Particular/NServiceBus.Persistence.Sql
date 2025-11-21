namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Reflection;
    using System.Linq.Expressions;

    /// <summary>
    /// Base class for all sagas being stored by the SQL Persistence. Replaces <see cref="Saga{TSagaData}"/>.
    /// </summary>
    public abstract class SqlSaga<TSagaData> : Saga
        where TSagaData : class, IContainSagaData
    {
        internal void VerifyNoConfigureHowToFindSaga()
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var type = GetType();
            var methodInfo = type.GetMethod("ConfigureHowToFindSaga", bindingFlags);
            if (methodInfo != null)
            {
                throw new Exception($"SqlSaga should only override ConfigureMapping(IMessagePropertyMapper) overridden and not ConfigureHowToFindSaga({nameof(IConfigureHowToFindSagaWithMessage)}). Saga: {type.FullName}.");
            }
        }

        /// <summary>
        /// Gets the name of the correlation property for <typeparamref name="TSagaData"/>.
        /// </summary>
        protected abstract string CorrelationPropertyName { get; }

        /// <summary>
        /// Gets the name of the transitional property for <typeparamref name="TSagaData"/>.
        /// Used to transition between different properties for saga correlation.
        /// </summary>
        protected virtual string TransitionalCorrelationPropertyName { get; }

        /// <summary>
        /// The name of the table to use when storing the current <see cref="SqlSaga{TSagaData}"/>.
        /// Will be appended to the value specified in <see cref="SqlPersistenceConfig.TablePrefix"/>.
        /// </summary>
        protected virtual string TableSuffix { get; }

        /// <summary>
        /// The saga's strongly typed data. Wraps <see cref="Saga.Entity" />.
        /// </summary>
        public TSagaData Data
        {
            get => (TSagaData)Entity;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                Entity = value;
            }
        }

        /// <summary>
        /// <see cref="Saga.ConfigureHowToFindSaga"/>. Do not override this method.
        /// </summary>
        protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage mapper)
        {
            VerifyNoConfigureHowToFindSaga();
            var propertyMapper = new PropertyMapper<TSagaData>(mapper, GetExpression(), GetType());
            ConfigureMapping(propertyMapper);
            ConfigureFinderMapping((IConfigureHowToFindSagaWithFinder)mapper);
        }

        /// <summary>
        /// Allows messages to be mapped to <see cref="CorrelationPropertyName"/>.
        /// </summary>
        protected abstract void ConfigureMapping(IMessagePropertyMapper mapper);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapper"></param>
        protected virtual void ConfigureFinderMapping(IConfigureHowToFindSagaWithFinder mapper)
        {
        }

        internal Expression<Func<TSagaData, object>> GetExpression()
        {
            if (CorrelationPropertyName == null)
            {
                return null;
            }

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