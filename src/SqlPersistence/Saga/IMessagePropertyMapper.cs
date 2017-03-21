namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Linq.Expressions;

    public interface IMessagePropertyMapper
    {
        /// <summary>
        /// Configures the mapping between <see cref="SqlSaga{TSagaData}.CorrelationPropertyName"/> and <typeparamref name="TMessage" />.
        /// </summary>
        /// <typeparam name="TMessage">The message type to map to.</typeparam>
        /// <param name="messageProperty">An <see cref="Expression{TDelegate}" /> that represents the message.</param>
        void MapMessage<TMessage>(Expression<Func<TMessage, object>> messageProperty);
    }
}