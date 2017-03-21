namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// For mapping saga correlation properties by using a <see cref="SqlSaga{TSagaData}.CorrelationPropertyName"/>. Used by <see cref="SqlSaga{TSagaData}.ConfigureHowToFindSaga"/>.
    /// </summary>
    public interface IMessagePropertyMapper
    {
        /// <summary>
        /// Configures the mapping between <see cref="SqlSaga{TSagaData}.CorrelationPropertyName"/> and <typeparamref name="TMessage" />.
        /// </summary>
        /// <typeparam name="TMessage">The message type to map to.</typeparam>
        /// <param name="messageProperty">An <see cref="Expression{TDelegate}" /> that represents the message property.</param>
        void MapMessage<TMessage>(Expression<Func<TMessage, object>> messageProperty);
    }
}