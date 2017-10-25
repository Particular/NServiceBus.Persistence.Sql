namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Extensibility;

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
                throw new Exception($"SqlSaga should only override ConfigureMapping(IMessagePropertyMapper) overridden and not ConfigureHowToFindSaga({nameof(IConfigureHowToFindSagaWithMessage)}). Saga: {type.FullName}.");
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
        /// Request for a timeout to occur at the given <see cref="T:System.DateTime" />.
        /// </summary>
        /// <param name="context">The context which is used to send the timeout.</param>
        /// <param name="timeoutId">Unique ID of the timeout.</param>
        /// <param name="at"><see cref="T:System.DateTime" /> to send timeout <paramref name="timeoutMessage" />.</param>
        /// <param name="timeoutMessage">The message to send after <paramref name="at" /> is reached.</param>
        protected Task RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext context, Guid timeoutId, DateTime at, TTimeoutMessageType timeoutMessage)
        {
            if (at.Kind == DateTimeKind.Unspecified)
            {
                throw new InvalidOperationException("Kind property of DateTime 'at' must be specified.");
            }

            VerifySagaCanHandleTimeout(timeoutMessage);

            var options = new SendOptions();

            options.DoNotDeliverBefore(at);
            options.RouteToThisEndpoint();

            SetTimeoutHeaders(options, timeoutId);

            return context.Send(timeoutMessage, options);
        }

        /// <summary>
        /// Request for a timeout to occur at the given <see cref="T:System.DateTime" />.
        /// </summary>
        /// <param name="context">The context which is used to send the timeout.</param>
        /// <param name="behavior">Timeout request behavior.</param>
        /// <param name="at"><see cref="T:System.DateTime" /> to send timeout <paramref name="timeoutMessage" />.</param>
        /// <param name="timeoutMessage">The message to send after <paramref name="at" /> is reached.</param>
        protected Task RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext context, TimeoutRequestBehavior behavior, DateTime at, TTimeoutMessageType timeoutMessage)
            where TTimeoutMessageType : class
        {
            if (at.Kind == DateTimeKind.Unspecified)
            {
                throw new InvalidOperationException("Kind property of DateTime 'at' must be specified.");
            }

            VerifySagaCanHandleTimeout(timeoutMessage);

            var options = new SendOptions();

            options.DoNotDeliverBefore(at);
            options.RouteToThisEndpoint();

            var timeoutId = HandleCancellableTimeout(context, behavior, timeoutMessage);

            SetTimeoutHeaders(options, timeoutId);

            return context.Send(timeoutMessage, options);
        }

        void VerifySagaCanHandleTimeout<TTimeoutMessageType>(TTimeoutMessageType timeoutMessage)
        {
            var canHandleTimeoutMessage = this is IHandleTimeouts<TTimeoutMessageType>;
            if (!canHandleTimeoutMessage)
            {
                var message = $"The type '{GetType().Name}' cannot request timeouts for '{timeoutMessage}' because it does not implement 'IHandleTimeouts<{typeof(TTimeoutMessageType).FullName}>'";
                throw new Exception(message);
            }
        }

        void SetTimeoutHeaders(ExtendableOptions options, Guid timeoutId)
        {
            options.SetHeader(Headers.SagaId, Entity.Id.ToString());
            options.SetHeader(Headers.IsSagaTimeoutMessage, bool.TrueString);
            options.SetHeader(Headers.SagaType, GetType().AssemblyQualifiedName);
            options.SetHeader("NServiceBus.Sql.TimeoutId", timeoutId.ToString());
        }

        /// <summary>
        /// Request for a timeout to occur within the given <see cref="T:System.TimeSpan" />.
        /// </summary>
        /// <param name="context">The context which is used to send the timeout.</param>
        /// <param name="behavior">Timeout request behavior.</param>
        /// <param name="within">Given <see cref="T:System.TimeSpan" /> to delay timeout message by.</param>
        /// <param name="timeoutMessage">The message to send after <paramref name="within" /> expires.</param>
        protected Task RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext context, TimeoutRequestBehavior behavior, TimeSpan within, TTimeoutMessageType timeoutMessage)
            where TTimeoutMessageType : class
        {
            if (timeoutMessage == null)
            {
                throw new ArgumentNullException(nameof(timeoutMessage));
            }

            VerifySagaCanHandleTimeout(timeoutMessage);

            var sendOptions = new SendOptions();

            sendOptions.DelayDeliveryWith(within);
            sendOptions.RouteToThisEndpoint();

            var timeoutId = HandleCancellableTimeout(context, behavior, timeoutMessage);

            SetTimeoutHeaders(sendOptions, timeoutId);

            return context.Send(timeoutMessage, sendOptions);
        }

        static Guid HandleCancellableTimeout<TTimeoutMessageType>(IMessageHandlerContext context, TimeoutRequestBehavior behavior, TTimeoutMessageType timeoutMessage) where TTimeoutMessageType : class
        {
            var timeoutId = Guid.NewGuid();
            var metadata = context.Extensions.GetOrCreate<SagaInstanceMetadata>();
            var key = timeoutMessage.GetType().FullName;
            // ReSharper disable once AssignNullToNotNullAttribute
            if (!metadata.PendingTimeouts.TryGetValue(key, out var pendingList))
            {
                pendingList = new List<string>();
                metadata.PendingTimeouts[key] = pendingList;
            }
            if (behavior == TimeoutRequestBehavior.CancelPrevious)
            {
                foreach (var previous in pendingList)
                {
                    if (!metadata.CanceledTimeouts.Contains(previous))
                    {
                        metadata.CanceledTimeouts.Add(previous);
                    }
                }
            }
            pendingList.Add(timeoutId.ToString());
            return timeoutId;
        }

        /// <summary>
        /// Request for a timeout to occur within the given <see cref="T:System.TimeSpan" />.
        /// </summary>
        /// <param name="context">The context which is used to send the timeout.</param>
        /// <param name="timeoutId">Unique ID of the timeout.</param>
        /// <param name="within">Given <see cref="T:System.TimeSpan" /> to delay timeout message by.</param>
        /// <param name="timeoutMessage">The message to send after <paramref name="within" /> expires.</param>
        protected Task RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext context, Guid timeoutId, TimeSpan within, TTimeoutMessageType timeoutMessage)
        {
            VerifySagaCanHandleTimeout(timeoutMessage);

            var sendOptions = new SendOptions();

            sendOptions.DelayDeliveryWith(within);
            sendOptions.RouteToThisEndpoint();

            SetTimeoutHeaders(sendOptions, timeoutId);

            return context.Send(timeoutMessage, sendOptions);
        }

        /// <summary>
        /// Cancels timeout with a given ID. That timeout will not be handled (will be ignored).
        /// </summary>
        /// <param name="context">The context which is used to send the timeout.</param>
        /// <param name="timeoutId">Unique ID of the timeout.</param>
        protected void CancelTimeout(IMessageHandlerContext context, Guid timeoutId)
        {
            var metadata = context.Extensions.GetOrCreate<SagaInstanceMetadata>();
            var idString = timeoutId.ToString();
            if (!metadata.CanceledTimeouts.Contains(idString))
            {
                metadata.CanceledTimeouts.Add(idString);
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
            var propertyMapper = new PropertyMapper<TSagaData>(mapper, GetExpression(), GetType());
            ConfigureMapping(propertyMapper);
        }

        /// <summary>
        /// Allows messages to be mapped to <see cref="CorrelationPropertyName"/>.
        /// </summary>
        protected abstract void ConfigureMapping(IMessagePropertyMapper mapper);

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