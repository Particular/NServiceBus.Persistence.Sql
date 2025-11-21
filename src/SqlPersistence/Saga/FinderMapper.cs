namespace NServiceBus.Persistence.Sql;

class FinderMapper<TSagaData>(IConfigureHowToFindSagaWithFinder configureHowToFindSagaWithFinder)
    : ISagaFinderMapper<TSagaData>
    where TSagaData : class, IContainSagaData
{
    void ISagaFinderMapper<TSagaData>.ConfigureMapping<TMessage, TSagaFinder>() =>
        configureHowToFindSagaWithFinder.ConfigureMapping<TSagaData, TMessage, TSagaFinder>();
}