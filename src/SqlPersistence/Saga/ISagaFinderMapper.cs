namespace NServiceBus.Persistence.Sql;

using Sagas;

/// <summary>
/// For mapping saga finders.
/// </summary>
public interface ISagaFinderMapper<TSagaData> where TSagaData : class, IContainSagaData
{
    /// <summary>
    /// Configures the mapping between saga and <typeparamref name="TMessage" />.
    /// </summary>
    /// <typeparam name="TMessage">The message type to map to.</typeparam>
    /// <typeparam name="TSagaFinder">The saga finder type.</typeparam>
    void ConfigureMapping<TMessage, TSagaFinder>() where TSagaFinder : class, ISagaFinder<TSagaData, TMessage>;
}