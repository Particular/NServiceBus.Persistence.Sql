namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// For mapping a messages property to a sagas correlation property. Used by <see cref="SqlSaga{TSagaData}.ConfigureMapping"/>.
    /// </summary>
    public interface IMessagePropertyMapper
    {
        /// <summary>
        /// Specify how to map between <typeparamref name="TMessage"/> and the correlation property on defined by <see cref="SqlSagaAttribute.CorrelationProperty"/>.
        /// </summary>
        /// <typeparam name="TMessage">The message type to map to.</typeparam>
        /// <param name="messageProperty">An <see cref="Expression"/> that represents the message property to map to.</param>
        void MapMessage<TMessage>(Expression<Func<TMessage, object>> messageProperty);
    }

}