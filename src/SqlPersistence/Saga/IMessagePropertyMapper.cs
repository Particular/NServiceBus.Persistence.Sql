namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// For mapping saga correlation properties by using a <see cref="SqlSaga{TSagaData}.CorrelationPropertyName"/>. Used by <see cref="SqlSaga{TSagaData}.ConfigureHowToFindSaga"/>.
    /// </summary>
    [ObsoleteEx(
        Message = "The SqlSaga base class is deprecated. Use 'NServiceBus.Saga<T>' with the 'mapper.MapSaga(…).ToMessage(…)' API. Other SqlSaga features can be added applying the 'SqlSagaAttribute' the saga class",
        RemoveInVersion = "10",
        TreatAsErrorFromVersion = "9")]
    public interface IMessagePropertyMapper
    {
        /// <summary>
        /// Configures the mapping between <see cref="SqlSaga{TSagaData}.CorrelationPropertyName"/> and <typeparamref name="TMessage" />.
        /// </summary>
        /// <typeparam name="TMessage">The message type to map to.</typeparam>
        /// <param name="messageProperty">An <see cref="Expression{TDelegate}"/> that represents the message property.</param>
        void ConfigureMapping<TMessage>(Expression<Func<TMessage, object>> messageProperty);
    }
}