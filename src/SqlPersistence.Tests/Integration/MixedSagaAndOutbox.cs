using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

[TestFixture]
public class MixedSagaAndOutbox
{
    [Test]
    public void RunSqlForSaga()
    {
        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(nameof(MixedSagaAndOutbox));
        var typesToScan = TypeScanner.NestedTypes<MixedSagaAndOutbox>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        endpointConfiguration.UseTransport<LearningTransport>();

        endpointConfiguration.EnableOutbox();

        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(MsSqlConnectionBuilder.Build);
        persistence.DisableInstaller();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Outbox>();

        var exception = Assert.ThrowsAsync<Exception>(() => Endpoint.Start(endpointConfiguration));
        Assert.IsTrue(exception.Message == "Sql Persistence must be enable for either both Sagas and Outbox, or neither.");
    }

    [Test]
    public void RunSqlForOutbox()
    {
        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(nameof(MixedSagaAndOutbox));
        var typesToScan = TypeScanner.NestedTypes<MixedSagaAndOutbox>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        endpointConfiguration.UseTransport<LearningTransport>();

        endpointConfiguration.EnableOutbox();

        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        persistence.ConnectionBuilder(MsSqlConnectionBuilder.Build);
        persistence.DisableInstaller();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Sagas>();

        var exception = Assert.ThrowsAsync<Exception>(() => Endpoint.Start(endpointConfiguration));
        Assert.IsTrue(exception.Message == "Sql Persistence must be enable for either both Sagas and Outbox, or neither.");
    }

    public class StartSagaMessage : IMessage
    {
        public Guid StartId { get; set; }
    }

    public class Saga1 : SqlSaga<Saga1.SagaData>,
        IAmStartedByMessages<StartSagaMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }

        public class SagaData : ContainSagaData
        {
            public Guid StartId { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.StartId);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(message => message.StartId);
        }
    }
}